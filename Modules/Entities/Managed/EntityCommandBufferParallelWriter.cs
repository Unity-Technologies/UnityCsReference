// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    public unsafe partial struct EntityCommandBuffer : IDisposable
    {
        /// <summary>An extension of EntityCommandBuffer that allows concurrent (deterministic) command buffer recording.</summary>
        /// <returns>The <see cref="ParallelWriter"/> that can be used to record commands in parallel.</returns>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter parallelWriter;

            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            parallelWriter.m_Safety0 = m_Safety0;
            AtomicSafetyHandle.UseSecondaryVersion(ref parallelWriter.m_Safety0);
            parallelWriter.m_BufferSafety = m_BufferSafety;
            parallelWriter.m_ArrayInvalidationSafety = m_ArrayInvalidationSafety;
            parallelWriter.m_SafetyReadOnlyCount = 0;
            parallelWriter.m_SafetyReadWriteCount = 3;

            if (m_Data->m_Allocator.ToAllocator == Allocator.Temp)
            {
                throw new InvalidOperationException($"{nameof(EntityCommandBuffer.ParallelWriter)} can not use Allocator.Temp; use the EntityCommandBufferSystem's RewindableAllocator instead");
            }
            parallelWriter.m_Data = m_Data;
            parallelWriter.m_ThreadIndex = -1;

            if (parallelWriter.m_Data != null)
            {
                parallelWriter.m_Data->InitForParallelWriter();
            }

            return parallelWriter;
        }


        /// <summary>
        /// Allows concurrent (deterministic) command buffer recording.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        [StructLayout(LayoutKind.Sequential)]
        unsafe public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction] internal EntityCommandBufferData* m_Data;

            internal AtomicSafetyHandle m_Safety0;
            internal AtomicSafetyHandle m_BufferSafety;
            internal AtomicSafetyHandle m_ArrayInvalidationSafety;
            internal int m_SafetyReadOnlyCount;
            internal int m_SafetyReadWriteCount;

            // NOTE: Until we have a way to safely batch, let's keep it off
            private const bool kBatchableCommand = false;

            //internal ref int m_EntityIndex;
            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private void CheckWriteAccess()
            {
                if (m_Data == null)
                    throw new NullReferenceException("The EntityCommandBuffer has not been initialized! The EntityCommandBuffer needs to be passed an Allocator when created!");
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
            }

            private EntityCommandBufferChain* ThreadChain => (m_ThreadIndex >= 0) ? &m_Data->m_ThreadedChains[m_ThreadIndex] : &m_Data->m_MainThreadChain;

            /// <summary>Records a command to create an entity with specified archetype.</summary>
            /// <remarks>At playback, this command will throw an error if the archetype contains the <see cref="Prefab"/> tag.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="archetype">The archetype of the new entity.</param>
            /// <returns>An entity that will be assigned a chunk when this EntityCommandBuffer is played back.</returns>
            /// <exception cref="ArgumentException">Throws if the archetype is null.</exception>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public Entity CreateEntity(int sortKey, EntityArchetype archetype)
            {
                archetype.CheckValidEntityArchetype();
                Entity entity;
                _CreateEntity(sortKey, archetype, &entity, 1);
                return entity;
            }

            /// <summary>Records a command to create an entity with no components.</summary>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <returns>An entity that will be assigned a chunk when this EntityCommandBuffer is played back.</returns>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public Entity CreateEntity(int sortKey)
            {
                EntityArchetype archetype = new EntityArchetype();
                Entity entity;
                _CreateEntity(sortKey, archetype, &entity, 1);
                return entity;
            }

            /// <summary>Records a command to create an array of entities with specified archetype.</summary>
            /// <remarks>At playback, this command throws an error if the archetype contains the <see cref="Prefab"/> tag.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="archetype">The archetype of the new entities.</param>
            /// <param name="entities">An array to hold the Entity objects needed to access the new entities.
            /// The length of the array determines how many entities are created.</param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void CreateEntity(int sortKey, EntityArchetype archetype, NativeArray<Entity> entities)
            {
                _CreateEntity(sortKey, archetype, (Entity*)entities.GetUnsafePtr(), entities.Length);
            }

            /// <summary>Records a command to create a specified number of entities with no components.</summary>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="count">The number of entities to create.</param>
            /// <param name="allocator">The allocator to use for the returned array.</param>
            /// <returns>An array of the created entities. Each entity is valid immediately, but Unity doesn't assign it a chunk until the Playback method runs.</returns>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public NativeArray<Entity> CreateEntity(int sortKey, int count, Allocator allocator)
            {
                EntityArchetype archetype = new EntityArchetype();
                NativeArray<Entity> entities = new NativeArray<Entity>(count, allocator);
                _CreateEntity(sortKey, archetype, (Entity*)entities.GetUnsafePtr(), count);
                return entities;
            }

            private void _CreateEntity(int sortKey, EntityArchetype archetype, Entity* entities, int count)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                // NOTE: Contention could be a performance problem especially on ARM
                // architecture. Maybe reserve a few indices for each job would be a better
                // approach or hijack the Version field of an Entity and store sortKey
                EntityComponentStore.s_entityStore.Data.AllocateEntities(entities, count);

                m_Data->AddCreateCommand(chain, sortKey, ECBCommand.CreateEntity, entities, count, archetype);
                chain->m_EntityCount += count;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            private static void CheckNotNull(Entity e)
            {
                if (Hint.Unlikely(e == Entity.Null))
                    throw new ArgumentNullException(nameof(e));
            }

            /// <summary>Records a command to create an entity with specified entity prefab.</summary>
            /// <remarks>An instantiated entity will have the same components and component values as the prefab entity, minus the Prefab tag component.
            /// At playback, this command will throw an error if the source entity was destroyed before playback.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity prefab.</param>
            /// <returns>An entity that will be assigned a chunk when this EntityCommandBuffer is played back.</returns>
            /// <exception cref="ArgumentNullException"> Thrown if Entity e is null and if safety checks are enabled.</exception>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public Entity Instantiate(int sortKey, Entity e)
            {
                CheckNotNull(e);

                CheckWriteAccess();
                var chain = ThreadChain;
                Entity newEntity;
                EntityComponentStore.s_entityStore.Data.AllocateEntities(&newEntity, 1);

                m_Data->AddEntityCommand(chain, sortKey, ECBCommand.InstantiateEntity, newEntity, e);
                chain->m_EntityCount += 1;
                return newEntity;
            }

            /// <summary>Records a command to create a NativeArray of entities with specified entity prefab.</summary>
            /// <remarks>An instantiated entity will have the same components and component values as the prefab entity, minus the Prefab tag component.
            /// At playback, this command will throw an error if the source entity was destroyed before playback.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity prefab.</param>
            /// <param name="entities">The NativeArray of entities that will be assigned chunks when this EntityCommandBuffer is played back.</param>
            /// <exception cref="ArgumentNullException"> Thrown if Entity e is null and if safety checks are enabled.</exception>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void Instantiate(int sortKey, Entity e, NativeArray<Entity> entities)
            {
                CheckNotNull(e);

                CheckWriteAccess();
                var chain = ThreadChain;
                EntityComponentStore.s_entityStore.Data.AllocateEntities(entities);
                m_Data->AddMultipleEntityCommand(chain, sortKey, ECBCommand.InstantiateEntity, entities, e);
                chain->m_EntityCount += entities.Length;
            }


            /// <summary>Records a command to destroy an entity.</summary>
            /// <remarks>At playback, this command will throw an error if any of the entities were destroyed between recording and playback,
            /// or if the entity has the <see cref="Prefab"/> tag.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity to destroy.</param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void DestroyEntity(int sortKey, Entity e)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityCommand(chain, sortKey, ECBCommand.DestroyEntity, Entity.Null, e);
            }

            /// <summary>Records a command to destroy a NativeArray of entities.</summary>
            /// <remarks>At playback, this command will do nothing if entities has a count of 0.
            /// This command will throw an error if any of the entities were destroyed between recording and playback,
            /// or if any of the entities have the <see cref="Prefab"/> tag.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities">The NativeArray of entities to destroy.</param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void DestroyEntity(int sortKey, NativeArray<Entity> entities)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                var entitiesCopy = m_Data->CloneEntitiesArray(entities);
                m_Data->AppendMultipleEntitiesCommand(chain, sortKey, ECBCommand.DestroyMultipleEntities, entitiesCopy, entities.Length);
            }

            /// <summary> Records a command to add component of type T to an entity. </summary>
            /// <remarks>At playback, if the entity already has this type of component, the value will just be set.
            /// Will throw an error if this entity is destroyed before playback,
            /// if T is type Entity or <see cref="Prefab"/>, or adding this componentType makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to have the component added. </param>
            /// <param name="component">The value to add on the new component in playback for the entity.</param>
            /// <typeparam name="T"> The type of component to add. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent<T>(int sortKey, Entity e, T component) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentTypeWithValueCommand(chain, sortKey, ECBCommand.AddComponent, e, component);
            }

            /// <summary> Records a command to add a component to an entity. </summary>
            /// <remarks>At playback, this command will do nothing if the entity already has the component.
            /// Will throw an error if this entity is destroyed before playback,
            /// if component type is type Entity or <see cref="Prefab"/>, or adding this componentType makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="typeIndex"> The TypeIndex of the component being set. </param>
            /// <param name="typeSize"> The Size of the type of the component being set. </param>
            /// <param name="componentDataPtr"> The pointer to the data of the component to be copied. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
            internal void UnsafeAddComponent(int sortKey, Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->UnsafeAddEntityComponentCommand(chain, sortKey, ECBCommand.AddComponent, e, typeIndex, typeSize, componentDataPtr);
            }

            /// <summary> Records a command to add component of type T to a NativeArray of entities. </summary>
            /// <remarks>At playback, if any entity already has this type of component, the value will just be set.
            /// Will throw an error if any entity is destroyed before playback,
            /// if T is type Entity or <see cref="Prefab"/>, or adding this componentType makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to have the component added. </param>
            /// <param name="component">The value to add on the new component in playback for all entities in the NativeArray.</param>
            /// <typeparam name="T"> The type of component to add. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent<T>(int sortKey, NativeArray<Entity> entities, T component) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                var entitiesCopy = m_Data->CloneEntitiesArray(entities);
                m_Data->AppendMultipleEntitiesComponentCommandWithValue(chain, sortKey,
                    ECBCommand.AddComponentForMultipleEntities, entitiesCopy, entities.Length, component);
            }

            /// <summary> Records a command to add component of type T to an entity. </summary>
            /// <remarks>At playback, this command will do nothing if the entity already has the component.
            /// Will throw an error if this entity is destroyed before playback,
            /// if T is type Entity or <see cref="Prefab"/>, or adding this componentType makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to have the component added. </param>
            /// <typeparam name="T"> The type of component to add. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent<T>(int sortKey, Entity e) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentTypeWithoutValueCommand(chain, sortKey, ECBCommand.AddComponent, e, ComponentType.ReadWrite<T>());
            }

            /// <summary> Records a command to add component of type T to a NativeArray of entities. </summary>
            /// <remarks>At playback, if an entity already has this component, it will be skipped.
            /// Will throw an error if any entity is destroyed before playback,
            /// if T is type Entity or <see cref="Prefab"/>, or adding this componentType makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to have the component added. </param>
            /// <typeparam name="T"> The type of component to add. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent<T>(int sortKey, NativeArray<Entity> entities) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                var entitiesCopy = m_Data->CloneEntitiesArray(entities);
                m_Data->AppendMultipleEntitiesComponentCommand(chain, sortKey,
                    ECBCommand.AddComponentForMultipleEntities, entitiesCopy, entities.Length,
                    ComponentType.ReadWrite<T>());
            }

            /// <summary> Records a command to add a component to an entity. </summary>
            /// <remarks>At playback, this command will do nothing if the entity already has the component.
            /// Will throw an error if this entity is destroyed before playback,
            /// if component type is type Entity or <see cref="Prefab"/>, or adding this componentType makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to get the additional component. </param>
            /// <param name="componentType"> The type of component to add. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent(int sortKey, Entity e, ComponentType componentType)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentTypeWithoutValueCommand(chain, sortKey, ECBCommand.AddComponent, e, componentType);
            }

            /// <summary> Records a command to add a component to a NativeArray of entities. </summary>
            /// <remarks>At playback, if an entity already has this component, it will be skipped.
            /// Will throw an error if any entity is destroyed before playback,
            /// if component type is type Entity or <see cref="Prefab"/>, or adding this componentType makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to have the component added. </param>
            /// <param name="componentType"> The type of component to add. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent(int sortKey, NativeArray<Entity> entities, ComponentType componentType)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                var entitiesCopy = m_Data->CloneEntitiesArray(entities);
                m_Data->AppendMultipleEntitiesComponentCommand(chain, sortKey,
                    ECBCommand.AddComponentForMultipleEntities, entitiesCopy, entities.Length, componentType);
            }

            /// <summary> Records a command to add one or more components to an entity. </summary>
            /// <remarks>At playback, it's not an error to include a component type that the entity already has.
            /// Will throw an error if this entity is destroyed before playback,
            /// if any component type is type Entity or <see cref="Prefab"/>, or adding a component type makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to get additional components. </param>
            /// <param name="typeSet"> The types of components to add. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent(int sortKey, Entity e, in ComponentTypeSet typeSet)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentTypesCommand(chain, sortKey,ECBCommand.AddMultipleComponents, e, typeSet);
            }

            /// <summary> Records a command to add one or more components to a NativeArray of entities. </summary>
            /// <remarks>At playback, it's not an error to include a component type that any of the entities already have.
            /// Will throw an error if any entity is destroyed before playback,
            /// if any component type is type Entity or <see cref="Prefab"/>, or adding a component type makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to have the components added. </param>
            /// <param name="typeSet"> The types of components to add. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddComponent(int sortKey, NativeArray<Entity> entities, in ComponentTypeSet typeSet)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                var entitiesCopy = m_Data->CloneEntitiesArray(entities);
                m_Data->AppendMultipleEntitiesMultipleComponentsCommand(chain, sortKey,
                    ECBCommand.AddMultipleComponentsForMultipleEntities, entitiesCopy, entities.Length, typeSet);
            }

            /// <summary>Records a command to add a dynamic buffer to an entity.</summary>
            /// <remarks>At playback, if the entity already has this type of dynamic buffer,
            /// this method sets the dynamic buffer contents. If the entity doesn't have a
            /// <see cref="DynamicBuffer{T}"/> component that stores elements of type T, then
            /// this method adds a DynamicBuffer component with the provided contents. If the
            /// entity is destroyed before playback, an error is thrown.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity to add the dynamic buffer to.</param>
            /// <typeparam name="T">The <see cref="IBufferElementData"/> type stored by the <see cref="DynamicBuffer{T}"/>.</typeparam>
            /// <returns>The <see cref="DynamicBuffer{T}"/> that will be added when the command plays back.</returns>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public DynamicBuffer<T> AddBuffer<T>(int sortKey, Entity e) where T : unmanaged, IBufferElementData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                return m_Data->CreateBufferCommand<T>(ECBCommand.AddBuffer, chain, sortKey, e, m_BufferSafety, m_ArrayInvalidationSafety);
            }

            /// <summary>Records a command to set a dynamic buffer on an entity.</summary>
            /// <remarks>At playback, this command throws an error if this entity is destroyed before playback,
            /// or if the entity doesn't have a <see cref="DynamicBuffer{T}"/> component storing elements of type T.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity to set the dynamic buffer on.</param>
            /// <typeparam name="T">The <see cref="IBufferElementData"/> type stored by the <see cref="DynamicBuffer{T}"/>.</typeparam>
            /// <returns>The <see cref="DynamicBuffer{T}"/> that will be set when the command plays back.</returns>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public DynamicBuffer<T> SetBuffer<T>(int sortKey, Entity e) where T : unmanaged, IBufferElementData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                return m_Data->CreateBufferCommand<T>(ECBCommand.SetBuffer, chain, sortKey, e, m_BufferSafety, m_ArrayInvalidationSafety);
            }

            /// <summary>Records a command to append a single element to the end of a dynamic buffer component.</summary>
            /// <remarks>At playback, this command throws an error if this entity is destroyed before playback,
            /// or if the entity doesn't have a <see cref="DynamicBuffer{T}"/> component storing elements of type T.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity to which the dynamic buffer belongs.</param>
            /// <param name="element">The new element to add to the <see cref="DynamicBuffer{T}"/> component.</param>
            /// <typeparam name="T">The <see cref="IBufferElementData"/> type stored by the <see cref="DynamicBuffer{T}"/>.</typeparam>
            /// <exception cref="InvalidOperationException">Thrown if the entity does not have a <see cref="DynamicBuffer{T}"/>
            /// component storing elements of type T at the time the entity command buffer executes this append-to-buffer command.</exception>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AppendToBuffer<T>(int sortKey, Entity e, T element) where T : struct, IBufferElementData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AppendToBufferCommand<T>(chain, sortKey, e, element);
            }

            /// <summary> Records a command to set a component value on an entity.</summary>
            /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
            /// if the entity doesn't have the component type,
            /// if the entity has the <see cref="Prefab"/> tag, or if T is zero sized.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to set the component value of. </param>
            /// <param name="component"> The component value to set. </param>
            /// <typeparam name="T"> The type of component to set. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void SetComponent<T>(int sortKey, Entity e, T component) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentTypeWithValueCommand(chain, sortKey, ECBCommand.SetComponent, e, component);
            }

            /// <summary> Records a command to set a component value on an entity.</summary>
            /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
            /// if the entity doesn't have the component type,
            /// if the entity has the <see cref="Prefab"/> tag, or if T is zero sized.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to set the component value of. </param>
            /// <param name="typeIndex"> The TypeIndex of the component being set. </param>
            /// <param name="typeSize"> The Size of the type of the component being set. </param>
            /// <param name="componentDataPtr"> The pointer to the data of the component to be copied. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
            internal void UnsafeSetComponent(int sortKey, Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->UnsafeAddEntityComponentCommand(chain, sortKey, ECBCommand.SetComponent, e, typeIndex, typeSize, componentDataPtr);
            }

            /// <summary>
            /// Records a command to add or remove the <see cref="Disabled"/> component. By default EntityQuery does not include entities containing the Disabled component.
            /// Enabled entities are processed by systems, disabled entities are not.
            ///
            /// If the entity was converted from a prefab and thus has a <see cref="LinkedEntityGroup"/> component, the entire group will be enabled or disabled.
            /// </summary>
            /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
            /// if the entity has the <see cref="Prefab"/> tag.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity whose component should be enabled or disabled.</param>
            /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
            [GenerateTestsForBurstCompatibility]
            public void SetEnabled(int sortKey, Entity e, bool value)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityEnabledCommand(chain, sortKey, ECBCommand.SetEntityEnabled, e, value);
            }

            /// <summary>
            /// Records a command to enable or disable a <see cref="ComponentType"/> on the specified <see cref="Entity"/>. This operation
            /// does not cause a structural change, or affect the value of the component. For the purposes
            /// of EntityQuery matching, an entity with a disabled component will behave as if it does not have that component.
            /// </summary>
            /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
            /// if the entity has the <see cref="Prefab"/> tag, or if the entity doesn't have the component type.</remarks>
            /// <typeparam name="T">The component type to enable or disable. This type must implement the
            /// <see cref="IEnableableComponent"/> interface.</typeparam>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity whose component should be enabled or disabled.</param>
            /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
            [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
            public void SetComponentEnabled<T>(int sortKey, Entity e, bool value) where T: struct, IEnableableComponent
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentEnabledCommand(chain, sortKey,
                    ECBCommand.SetComponentEnabled, e, TypeManager.GetTypeIndex<T>(), value);
            }
            /// <summary>
            /// Records a command to enable or disable a <see cref="ComponentType"/> on the specified <see cref="Entity"/>. This operation
            /// does not cause a structural change, or affect the value of the component. For the purposes
            /// of EntityQuery matching, an entity with a disabled component will behave as if it does not have that component.
            /// </summary>
            /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
            /// if the entity has the <see cref="Prefab"/> tag, or if the entity doesn't have the component type.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity whose component should be enabled or disabled.</param>
            /// <param name="componentType">The component type to enable or disable. This type must implement the
            /// <see cref="IEnableableComponent"/> interface.</param>
            /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
            public void SetComponentEnabled(int sortKey, Entity e, ComponentType componentType, bool value)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentEnabledCommand(chain, sortKey,
                    ECBCommand.SetComponentEnabled, e, componentType.TypeIndex, value);
            }

            /// <summary> Records a command to set a name of an entity if Debug Names is enabled.</summary>
            /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
            /// if the entity has the <see cref="Prefab"/> tag, or if the EntityNameStore has reached its limit.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to set the name value of. </param>
            /// <param name="name"> The name to set. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void SetName(int sortKey, Entity e, in FixedString64Bytes name)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityNameCommand(chain, sortKey, ECBCommand.SetName, e, name);
            }

            /// <summary> Records a command to remove component of type T from an entity. </summary>
            /// <remarks>At playback, it's not an error if the entity doesn't have component T.
            /// Will throw an error if this entity is destroyed before playback,
            /// or if T is type Entity or <see cref="Prefab"/>.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to have the component removed. </param>
            /// <typeparam name="T"> The type of component to remove. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void RemoveComponent<T>(int sortKey, Entity e)
            {
                RemoveComponent(sortKey, e, ComponentType.ReadWrite<T>());
            }

            /// <summary> Records a command to remove component of type T from a NativeArray of entities. </summary>
            /// <remarks>At playback, it's not an error if any entity doesn't have component T.
            /// Will throw an error if one of these entities is destroyed before playback,
            /// or if T is type Entity or <see cref="Prefab"/>.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to have the component removed. </param>
            /// <typeparam name="T"> The type of component to remove. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void RemoveComponent<T>(int sortKey, NativeArray<Entity> entities)
            {
                RemoveComponent(sortKey, entities, ComponentType.ReadWrite<T>());
            }

            /// <summary> Records a command to remove a component from an entity. </summary>
            /// <remarks>At playback, it's not an error if the entity doesn't have the component type.
            /// Will throw an error if this entity is destroyed before playback,
            /// or if the component type is Entity or <see cref="Prefab"/>.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to have the component removed. </param>
            /// <param name="componentType"> The type of component to remove. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void RemoveComponent(int sortKey, Entity e, ComponentType componentType)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentTypeWithoutValueCommand(chain, sortKey, ECBCommand.RemoveComponent, e, componentType);
            }

            /// <summary> Records a command to remove one or more components from a NativeArray of entities. </summary>
            /// <remarks>At playback, it's not an error if any entity doesn't have the component type.
            /// Will throw an error if one of these entities is destroyed before playback,
            /// or if the component type is Entity or <see cref="Prefab"/>.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to have the component removed. </param>
            /// <param name="componentType"> The type of component to remove. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void RemoveComponent(int sortKey, NativeArray<Entity> entities, ComponentType componentType)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                var entitiesCopy = m_Data->CloneEntitiesArray(entities);
                m_Data->AppendMultipleEntitiesComponentCommand(chain, sortKey,
                    ECBCommand.RemoveComponentForMultipleEntities, entitiesCopy, entities.Length, componentType);
            }

            /// <summary> Records a command to remove one or more components from an entity. </summary>
            /// <remarks>At playback, it's not an error if the entity doesn't have one of the component types.
            /// Will throw an error if this entity is destroyed before playback,
            /// or if any of the component types are Entity or <see cref="Prefab"/>.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to have the components removed. </param>
            /// <param name="typeSet"> The types of components to remove. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void RemoveComponent(int sortKey, Entity e, in ComponentTypeSet typeSet)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                m_Data->AddEntityComponentTypesCommand(chain, sortKey,ECBCommand.RemoveMultipleComponents, e, typeSet);
            }

            /// <summary> Records a command to remove one or more components from a NativeArray of entities. </summary>
            /// <remarks>At playback, it's not an error if any entity doesn't have one of the component types.
            /// Will throw an error if one of these entities is destroyed before playback,
            /// or if any of the component types are Entity or <see cref="Prefab"/>.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to have components removed. </param>
            /// <param name="typeSet"> The types of components to remove. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void RemoveComponent(int sortKey, NativeArray<Entity> entities, in ComponentTypeSet typeSet)
            {
                CheckWriteAccess();
                var chain = ThreadChain;
                var entitiesCopy = m_Data->CloneEntitiesArray(entities);
                m_Data->AppendMultipleEntitiesMultipleComponentsCommand(chain, sortKey,
                    ECBCommand.RemoveMultipleComponentsForMultipleEntities, entitiesCopy, entities.Length, typeSet);
            }

            /// <summary> Records a command to add an unmanaged shared component value on an entity.</summary>
            /// <remarks>At playback, this command throws an error if this entity is destroyed before playback,
            /// if adding this shared component exceeds the maximum number of shared components,
            /// or adding a component type makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to add the shared component value to. </param>
            /// <param name="sharedComponent"> The shared component value to add. </param>
            /// <typeparam name="T"> The type of shared component to add. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddSharedComponent<T>(int sortKey, Entity e, T sharedComponent)
                where T : unmanaged, ISharedComponentData
            {
                CheckWriteAccess();
                var componentData = IsDefaultObjectUnmanaged(ref sharedComponent, out var hashCode)
                    ? null
                    : UnsafeUtility.AddressOf(ref sharedComponent);
                m_Data->AddEntityUnmanagedSharedComponentCommand<T>(ThreadChain, sortKey, ECBCommand.AddUnmanagedSharedComponentData, e, hashCode, componentData);
            }

            /// <summary> Records a command to add an unmanaged shared component value on a NativeArray of entities.</summary>
            /// <remarks>At playback, this command throws an error if any entity is destroyed before playback,
            /// if adding this shared component exceeds the maximum number of shared components,
            /// or adding a component type makes the archetype too large.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to add the shared component value to. </param>
            /// <param name="sharedComponent"> The shared component value to add. </param>
            /// <typeparam name="T"> The type of shared component to add. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void AddSharedComponent<T>(int sortKey, NativeArray<Entity> entities, T sharedComponent)
                where T : unmanaged, ISharedComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;

                int hashCode;
                var entitiesCopy =
                    m_Data->CloneEntitiesArray(entities);
                var isdefault = IsDefaultObjectUnmanaged(ref sharedComponent, out hashCode);

                m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                    chain,
                    sortKey,
                    ECBCommand.AddUnmanagedSharedComponentValueForMultipleEntities,
                    entitiesCopy,
                    entities.Length,
                    hashCode,
                    isdefault ? null : UnsafeUtility.AddressOf(ref sharedComponent));
            }

            /// <summary> Records a command to set an unmanaged shared component value on an entity.</summary>
            /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
            /// if the entity has the <see cref="Prefab"/> tag, or if the entity doesn't have the shared component type.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e"> The entity to set the shared component value of. </param>
            /// <param name="sharedComponent"> The shared component value to set. </param>
            /// <typeparam name="T"> The type of shared component to set. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void SetSharedComponent<T>(int sortKey, Entity e, T sharedComponent) where T : unmanaged, ISharedComponentData
            {
                CheckWriteAccess();

                var componentData = IsDefaultObjectUnmanaged(ref sharedComponent, out var hashCode) ? null : UnsafeUtility.AddressOf(ref sharedComponent);
                m_Data->AddEntityUnmanagedSharedComponentCommand<T>(
                    ThreadChain,
                    sortKey,
                    ECBCommand.SetUnmanagedSharedComponentData,
                    e,
                    hashCode,
                    componentData);
            }

            /// <summary>
            /// Only for inserting a non-default value
            /// </summary>
            internal void UnsafeSetSharedComponentNonDefault(int sortKey, Entity e, void* componentDataPtr, TypeIndex typeIndex)
            {
                CheckWriteAccess();

                m_Data->AddEntityUnmanagedSharedComponentCommand(
                    ThreadChain,
                    sortKey,
                    ECBCommand.SetUnmanagedSharedComponentData,
                    e,
                    TypeManager.SharedComponentGetHashCode(componentDataPtr, typeIndex),
                    typeIndex,
                    TypeManager.GetTypeInfo(typeIndex).TypeSize,
                    componentDataPtr);
            }

            /// <summary> Records a command to set a shared component value on a NativeArray of entities.</summary>
            /// <remarks> At playback, this command throws an error if any entity is destroyed before playback,
            /// if any entity has the <see cref="Prefab"/> tag, or if any entity doesn't have the shared component type.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="entities"> The NativeArray of entities to set the shared component value of. </param>
            /// <param name="sharedComponent"> The shared component value to set. </param>
            /// <typeparam name="T"> The type of shared component to set. </typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            public void SetSharedComponent<T>(int sortKey, NativeArray<Entity> entities, T sharedComponent)
                where T : unmanaged, ISharedComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;

                int hashCode;
                var entitiesCopy =
                    m_Data->CloneEntitiesArray(entities);
                var isDefaultObject = IsDefaultObjectUnmanaged(ref sharedComponent, out hashCode);

                m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                    chain,
                    sortKey,
                    ECBCommand.SetUnmanagedSharedComponentValueForMultipleEntities,
                    entitiesCopy,
                    entities.Length,
                    hashCode,
                    isDefaultObject ? null : UnsafeUtility.AddressOf(ref sharedComponent));
            }

            /// <summary>Records a command that adds a component to an entity's <see cref="LinkedEntityGroup"/> based on an <see cref="EntityQueryMask"/>.
            /// Entities in the <see cref="LinkedEntityGroup"/> that don't match the mask will be skipped safely.</summary>
            /// <remarks>At playback, this command throws an error if the entity is destroyed before playback,
            /// or if any of the matching linked entities cannot add the component.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity whose LinkedEntityGroup will be referenced.</param>
            /// <param name="mask">The EntityQueryMask that is used to determine which linked entities to add the component to.
            /// Note that EntityQueryMask ignores all query filtering (including chunk filtering and enableable components),
            /// and may thus match more entities than expected.</param>
            /// <param name="component"> The component value to set. </param>
            /// <typeparam name="T"> The type of component to add.</typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
            public void AddComponentForLinkedEntityGroup<T>(int sortKey, Entity e, EntityQueryMask mask, T component) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;

                m_Data->AddLinkedEntityGroupComponentCommand(chain, sortKey, ECBCommand.AddComponentLinkedEntityGroup, mask, e, component);
            }

            /// <summary>Records a command that adds a component to an entity's <see cref="LinkedEntityGroup"/> based on an <see cref="EntityQueryMask"/>.
            /// Entities in the <see cref="LinkedEntityGroup"/> that don't match the mask will be skipped safely.</summary>
            /// <remarks>At playback, this command throws an error if the entity is destroyed before playback,
            /// or if any of the matching linked entities cannot add the component.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity whose LinkedEntityGroup will be referenced.</param>
            /// <param name="mask">The EntityQueryMask that is used to determine which linked entities to add the component to.
            /// Note that EntityQueryMask ignores all query filtering (including chunk filtering and enableable components),
            /// and may thus match more entities than expected.</param>
            /// <param name="componentType"> The component type to add. </param>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
            public void AddComponentForLinkedEntityGroup(int sortKey, Entity e, EntityQueryMask mask, ComponentType componentType)
            {
                CheckWriteAccess();
                var chain = ThreadChain;

                m_Data->AddLinkedEntityGroupTypeCommand(chain, sortKey, ECBCommand.AddComponentLinkedEntityGroup, mask, e, componentType);
            }

            /// <summary>Records a command that sets a component for an entity's <see cref="LinkedEntityGroup"/> based on an <see cref="EntityQueryMask"/>.
            /// Entities in the <see cref="LinkedEntityGroup"/> that don't match the mask will be skipped safely.</summary>
            /// <remarks>At playback, this command throws an error if the entity is destroyed before playback,
            /// if the entity has the <see cref="Prefab"/> tag, or if any of the matching linked entities do not already have the component.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity whose LinkedEntityGroup will be referenced.</param>
            /// <param name="mask">The EntityQueryMask that is used to determine which linked entities to set the component for.
            /// Note that EntityQueryMask ignores all query filtering (including chunk filtering and enableable components),
            /// and may thus match more entities than expected.</param>
            /// <param name="component"> The component value to set. </param>
            /// <typeparam name="T"> The type of component to add.</typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
            public void SetComponentForLinkedEntityGroup<T>(int sortKey, Entity e, EntityQueryMask mask, T component) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;

                m_Data->AddLinkedEntityGroupComponentCommand(chain, sortKey, ECBCommand.SetComponentLinkedEntityGroup, mask, e, component);
            }

            /// <summary>Records a command that replaces a component value for an entity's <see cref="LinkedEntityGroup"/>.
            /// Entities in the <see cref="LinkedEntityGroup"/> that don't have the component will be skipped safely.</summary>
            /// <remarks>At playback, this command throws an error if the entity is destroyed before playback.</remarks>
            /// <param name="sortKey">A unique index for each set of commands added to this EntityCommandBuffer
            /// across all parallel jobs writing commands to this buffer. The <see cref="ChunkIndexInQuery"/> provided by
            /// <see cref="IJobEntity"/> is an appropriate value to use for this parameter. In an <see cref="IJobChunk"/>
            /// pass the 'unfilteredChunkIndex' value from <see cref="IJobChunk.Execute"/>.</param>
            /// <param name="e">The entity whose LinkedEntityGroup will be referenced.</param>
            /// <param name="component"> The component value to set. </param>
            /// <typeparam name="T"> The type of component to add.</typeparam>
            /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
            /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
            public void ReplaceComponentForLinkedEntityGroup<T>(int sortKey, Entity e, T component) where T : unmanaged, IComponentData
            {
                CheckWriteAccess();
                var chain = ThreadChain;

                m_Data->AddEntityComponentTypeWithValueCommand(chain, sortKey, ECBCommand.ReplaceComponentLinkedEntityGroup, e, component);
            }
        }

    }
}
