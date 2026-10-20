// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using Assert = UnityEngine.Assertions.AssertCore;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;


namespace Unity.Entities
{
    public unsafe partial struct EntityCommandBuffer : IDisposable
    {
        // TODO(michalb): bugfix for https://jira.unity3d.com/browse/BUR-1767, remove when burst is upgraded to 1.7.2.
        static readonly ProfilerMarker k_ProfileEcbPlayback = new ProfilerMarker("EntityCommandBuffer.Playback");
        static readonly ProfilerMarker k_ProfileEcbDispose = new ProfilerMarker("EntityCommandBuffer.Dispose");

        /// <summary>
        /// Controls whether this command buffer should play back.
        /// </summary>
        ///
        /// This property is normally true, but can be useful to prevent
        /// the buffer from playing back when the user code is not in control
        /// of the site of playback.
        ///
        /// For example, is a buffer has been acquired from an EntityCommandBufferSystem and partially
        /// filled in with data, but it is discovered that the work should be aborted,
        /// this property can be set to false to prevent the buffer from playing back.
        public bool ShouldPlayback
        {
            get { return m_Data != null ? m_Data->m_ShouldPlayback : false; }
            set { if (m_Data != null) m_Data->m_ShouldPlayback = value; }
        }

        /// <summary>
        /// Play back all recorded operations against an entity manager.
        /// </summary>
        /// <param name="mgr">The entity manager that will receive the operations</param>
        public void Playback(EntityManager mgr)
        {
            PlaybackInternal(mgr.GetCheckedEntityDataAccess());
        }

        /// <summary>
        /// Play back all recorded operations with an exclusive entity transaction.
        /// <seealso cref="EntityManager.BeginExclusiveEntityTransaction"/>.
        /// </summary>
        /// <param name="mgr">The exclusive entity transaction that will process the operations</param>
        public void Playback(ExclusiveEntityTransaction mgr)
        {
            PlaybackInternal(mgr.EntityManager.GetCheckedEntityDataAccess());
        }

        void PlaybackInternal(EntityDataAccess* mgr)
        {
            EnforceSingleThreadOwnership();

            if (!ShouldPlayback || m_Data == null)
                return;
            if (m_Data != null && m_Data->m_DidPlayback)
            {
                throw new InvalidOperationException(
                    "Attempt to call Playback() on an EntityCommandBuffer that has already been played back.\nAn EntityCommandBuffer can only be played back once.");
            }

            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_BufferSafety);
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_ArrayInvalidationSafety);

            k_ProfileEcbPlayback.Begin();

            if (ENABLE_PRE_PLAYBACK_VALIDATION)
            {
                var walker = new EcbWalker<PrePlaybackValidationProcessor>(default, ECBProcessorType.PrePlaybackValidationProcessor);
                walker.processor.Init(mgr, m_Data, in OriginSystemHandle);
                walker.processor.playbackProcessor.ecbSafetyHandle = m_Safety0;
                walker.WalkChains(this);
                walker.processor.Cleanup();
                PassedPrePlaybackValidation = 1;
            }
            else if (PLAYBACK_WITH_TRACE)
            {
                var walker = new EcbWalker<PlaybackWithTraceProcessor>(default, ECBProcessorType.PlaybackWithTraceProcessor);
                walker.processor.Init(mgr, m_Data, in OriginSystemHandle);
                walker.processor.playbackProcessor.ecbSafetyHandle = m_Safety0;
                walker.WalkChains(this);
                walker.processor.Cleanup();
            }
            else
            {
                var walker = new EcbWalker<PlaybackProcessor>(default, ECBProcessorType.PlaybackProcessor);
                walker.processor.Init(mgr, m_Data, in OriginSystemHandle);
                walker.processor.ecbSafetyHandle = m_Safety0;
                walker.WalkChains(this);
                walker.processor.Cleanup();
            }

            m_Data->m_DidPlayback = true;
            k_ProfileEcbPlayback.End();
        }

        // This enum is used by the ECBInterop to allow us to have generic chain walking code
        // Each IEcbProcessor should have a type here
        // ECBInterop._ProcessChainChunk(...) needs to be updated with the new option as well
        internal enum ECBProcessorType
        {
            PlaybackProcessor,
            DebugViewProcessor,
            PlaybackWithTraceProcessor,
            PrePlaybackValidationProcessor,
        }

        internal interface IEcbProcessor
        {
            public void DestroyEntity(BasicCommand* header);
            public void RemoveComponent(BasicCommand* header);
            public void RemoveMultipleComponents(BasicCommand* header);
            public void CreateEntity(ECBPlaybackState* playbackState, BasicCommand* header);
            public void InstantiateEntity(BasicCommand* header);
            public void AddComponent(BasicCommand* header);
            public void AddMultipleComponents(BasicCommand* header);
            public void SetComponent(BasicCommand* header);
            public void SetEnabled(BasicCommand* header);
            public void SetComponentEnabled(BasicCommand* header);
            public void SetName(BasicCommand* header);
            public void AddBuffer(BasicCommand* header);
            public void SetBuffer(BasicCommand* header);
            public void AppendToBuffer(BasicCommand* header);
            public void AddComponentForEntityQuery(BasicCommand* header);
            public void AddComponentForMultipleEntities(BasicCommand* header);
            public void RemoveComponentForEntityQuery(BasicCommand* header);
            public void RemoveComponentForMultipleEntities(BasicCommand* header);
            public void AddMultipleComponentsForMultipleEntities(BasicCommand* header);
            public void AddMultipleComponentsForEntityQuery(BasicCommand* header);
            public void RemoveMultipleComponentsForMultipleEntities(BasicCommand* header);
            public void RemoveMultipleComponentsForEntityQuery(BasicCommand* header);
            public void DestroyMultipleEntities(BasicCommand* header);
            public void DestroyForEntityQuery(BasicCommand* header);
            public void AddComponentLinkedEntityGroup(BasicCommand* header);
            public void SetComponentLinkedEntityGroup(BasicCommand* header);
            public void ReplaceComponentLinkedEntityGroup(BasicCommand* header);
            public void AddUnmanagedSharedComponentData(BasicCommand* header);
            public void SetUnmanagedSharedComponentData(BasicCommand* header);
            public void AddUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header);
            public void AddUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header);
            public void SetUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header);
            public void SetUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header);

            public ECBProcessorType ProcessorType { get; }
        }

        internal struct EcbWalker<T> where T: unmanaged, IEcbProcessor {
            public T processor;
            public ECBProcessorType processorType;
            public EcbWalker(T ecbProcessor, ECBProcessorType type)
            {
                processor = ecbProcessor;
                processorType = type;
            }
            public void WalkChains(EntityCommandBuffer ecb)
            {
                EntityCommandBufferData* data = ecb.m_Data;
                // Walk all chains (Main + Threaded) and build a NativeArray of PlaybackState objects.
                // Only chains with non-null Head pointers will be included.
                if (data->m_RecordedChainCount <= 0)
                    return;

                ECBPlaybackState* playbackState = stackalloc ECBPlaybackState[1];
                playbackState->CurrentChunk = ChunkIndex.Null;
                playbackState->CurrentChunkArchetype = null;

                fixed (void* pThis = &this)
                {
                    var chainStates = stackalloc ECBChainPlaybackState[data->m_RecordedChainCount];

                    playbackState->ChainPlaybackState = chainStates;

                    int initialChainCount = 0;
                    for (var chain = &data->m_MainThreadChain; chain != null; chain = chain->m_NextChain)
                    {
                        if (chain->m_Head != null)
                        {
#pragma warning disable 728
                            chainStates[initialChainCount++] = new ECBChainPlaybackState
                            {
                                Chunk = chain->m_Head,
                                Offset = 0,
                                NextSortKey = chain->m_Head->BaseSortKey,
                                CanBurstPlayback = chain->m_CanBurstPlayback
                            };
#pragma warning restore 728
                        }
                    }

                    if (data->m_ThreadedChains != null)
                    {
                        int maxThreadCount = JobsUtility.ThreadIndexCount;
                        for (int i = 0; i < maxThreadCount; ++i)
                        {
                            for (var chain = &data->m_ThreadedChains[i]; chain != null; chain = chain->m_NextChain)
                            {
                                if (chain->m_Head != null)
                                {
#pragma warning disable 728
                                    chainStates[initialChainCount++] = new ECBChainPlaybackState
                                    {
                                        Chunk = chain->m_Head,
                                        Offset = 0,
                                        NextSortKey = chain->m_Head->BaseSortKey,
                                        CanBurstPlayback = chain->m_CanBurstPlayback
                                    };
#pragma warning restore 728
                                }
                            }
                        }
                    }
                    if (data->m_RecordedChainCount != initialChainCount)
                        Assert.IsTrue(false,
                            $"RecordedChainCount ({data->m_RecordedChainCount}) != initialChainCount ({initialChainCount}");

                    using (ECBChainPriorityQueue chainQueue = new ECBChainPriorityQueue(chainStates,
                        data->m_RecordedChainCount, Allocator.Temp))
                    {
                        ECBChainHeapElement currentElem = chainQueue.Pop();

                        while (currentElem.ChainIndex != -1)
                        {
                            ECBChainHeapElement nextElem = chainQueue.Peek();

                            var chunk = chainStates[currentElem.ChainIndex].Chunk;
                            var off = chainStates[currentElem.ChainIndex].Offset;
                            if (chunk == null)
                                Assert.IsTrue(false, $"chainStates[{currentElem.ChainIndex}].Chunk is null.");
                            if (off < 0 || off >= chunk->Used)
                                Assert.IsTrue(false, $"chainStates[{currentElem.ChainIndex}].Offset is invalid: {off}. Should be between 0 and {chunk->Used}");

                            if (chainStates[currentElem.ChainIndex].CanBurstPlayback)
                            {
                                // Bursting PlaybackChain
                                ECBInterop.ProcessChainChunk(pThis, (int)processorType, playbackState,
                                    currentElem.ChainIndex, nextElem.ChainIndex);
                            }
                            else
                            {
                                // Non-Bursted PlaybackChain
                                ECBInterop._ProcessChainChunk(pThis, (int)processorType, playbackState,
                                    currentElem.ChainIndex, nextElem.ChainIndex);
                            }

                            if (chainStates[currentElem.ChainIndex].Chunk == null)
                            {
                                chainQueue.Pop(); // ignore return value; we already have it as nextElem
                            }
                            else
                            {
                                currentElem.SortKey = chainStates[currentElem.ChainIndex].NextSortKey;
                                chainQueue.ReplaceTop(currentElem);
                            }

                            currentElem = nextElem;
                        }
                    }
                }
            }

            internal void ProcessChain(ECBPlaybackState* playbackState, int currentChain,
                int nextChain)
            {
                ECBChainPlaybackState* chainStates = playbackState->ChainPlaybackState;

                int nextChainSortKey = (nextChain != -1) ? chainStates[nextChain].NextSortKey : -1;
                var chunk = chainStates[currentChain].Chunk;
                var off = chainStates[currentChain].Offset;

                while (chunk != null)
                {
                    var buf = (byte*) chunk + sizeof(ECBChunk);
                    while (off < chunk->Used)
                    {
                        var header = (BasicCommand*) (buf + off);
                        if (nextChain != -1 && header->SortKey > nextChainSortKey)
                        {
                            // early out because a different chain needs to playback
                            var state = chainStates[currentChain];
                            state.Chunk = chunk;
                            state.Offset = off;
                            state.NextSortKey = header->SortKey;
                            chainStates[currentChain] = state;
                            return;
                        }

                        ProcessUnmanagedCommand(playbackState, header);

                        off += header->TotalSize;
                    }

                    // Reached the end of a chunk; advance to the next one
                    chunk = chunk->Next;
                    off = 0;
                }

                // Reached the end of the chain; update its playback state to make sure it's ignored
                // for the remainder of playback.
                {
                    var state = chainStates[currentChain];
                    state.Chunk = null;
                    state.Offset = 0;
                    state.NextSortKey = Int32.MinValue;
                    chainStates[currentChain] = state;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ProcessUnmanagedCommand(ECBPlaybackState* playbackState, BasicCommand* header)
            {
                switch ((ECBCommand)header->CommandType)
                {
                    case ECBCommand.InstantiateEntity:
                        processor.InstantiateEntity(header);
                        return;
                    case ECBCommand.CreateEntity:
                        processor.CreateEntity(playbackState, header);
                        return;
                    case ECBCommand.DestroyEntity:
                        processor.DestroyEntity(header);
                        return;

                    case ECBCommand.AddComponent:
                        processor.AddComponent(header);
                        return;
                    case ECBCommand.AddMultipleComponents:
                        processor.AddMultipleComponents(header);
                        return;

                    case ECBCommand.RemoveComponent:
                        processor.RemoveComponent(header);
                        return;

                    case ECBCommand.RemoveMultipleComponents:
                        processor.RemoveMultipleComponents(header);
                        return;

                    case ECBCommand.SetComponent:
                        processor.SetComponent(header);
                        return;

                    case ECBCommand.SetEntityEnabled:
                        processor.SetEnabled(header);
                        return;

                    case ECBCommand.SetComponentEnabled:
                        processor.SetComponentEnabled(header);
                        return;


                    case ECBCommand.SetName:
                        processor.SetName(header);
                        return;


                    case ECBCommand.AddBuffer:
                        processor.AddBuffer(header);
                        return;

                    case ECBCommand.SetBuffer:
                        processor.SetBuffer(header);
                        return;

                    case ECBCommand.AppendToBuffer:
                        processor.AppendToBuffer(header);
                        return;

                    case ECBCommand.AddComponentForEntityQuery:
                        processor.AddComponentForEntityQuery(header);
                        return;

                    case ECBCommand.AddComponentForMultipleEntities:
                        processor.AddComponentForMultipleEntities(header);
                        return;

                    case ECBCommand.RemoveComponentForEntityQuery:
                        processor.RemoveComponentForEntityQuery(header);
                        return;

                    case ECBCommand.RemoveComponentForMultipleEntities:
                        processor.RemoveComponentForMultipleEntities(header);
                        return;

                    case ECBCommand.AddMultipleComponentsForMultipleEntities:
                        processor.AddMultipleComponentsForMultipleEntities(header);
                        return;

                    case ECBCommand.AddMultipleComponentsForEntityQuery:
                        processor.AddMultipleComponentsForEntityQuery(header);
                        return;

                    case ECBCommand.RemoveMultipleComponentsForMultipleEntities:
                        processor.RemoveMultipleComponentsForMultipleEntities(header);
                        return;

                    case ECBCommand.RemoveMultipleComponentsForEntityQuery:
                        processor.RemoveMultipleComponentsForEntityQuery(header);
                        return;

                    case ECBCommand.DestroyMultipleEntities:
                        processor.DestroyMultipleEntities(header);
                        return;
                    case ECBCommand.DestroyForEntityQuery:
                        processor.DestroyForEntityQuery(header);
                        return;

                    case ECBCommand.AddUnmanagedSharedComponentData:
                        processor.AddUnmanagedSharedComponentData(header);
                        return;
                    case ECBCommand.SetUnmanagedSharedComponentData:
                        processor.SetUnmanagedSharedComponentData(header);
                        return;
                    case ECBCommand.AddUnmanagedSharedComponentValueForMultipleEntities:
                        processor.AddUnmanagedSharedComponentValueForMultipleEntities(header);
                        return;
                    case ECBCommand.SetUnmanagedSharedComponentValueForMultipleEntities:
                        processor.SetUnmanagedSharedComponentValueForMultipleEntities(header);
                        return;
                    case ECBCommand.AddUnmanagedSharedComponentValueForEntityQuery:
                        processor.AddUnmanagedSharedComponentValueForEntityQuery(header);
                        return;
                    case ECBCommand.SetUnmanagedSharedComponentValueForEntityQuery:
                        processor.SetUnmanagedSharedComponentValueForEntityQuery(header);
                        return;

                    case ECBCommand.AddComponentLinkedEntityGroup:
                        processor.AddComponentLinkedEntityGroup(header);
                        return;

                    case ECBCommand.SetComponentLinkedEntityGroup:
                        processor.SetComponentLinkedEntityGroup(header);
                        return;

                    case ECBCommand.ReplaceComponentLinkedEntityGroup:
                        processor.ReplaceComponentLinkedEntityGroup(header);
                        return;
                }

                // Every command type is handled above, so reaching here means a corrupt command stream.
                throw new InvalidOperationException("Invalid ECB command type encountered during playback.");
            }
        }

        internal struct PlaybackProcessor : IEcbProcessor
        {
            public EntityDataAccess* mgr;
            public EntityComponentStore.ArchetypeChanges archetypeChanges;
            public ECBSharedPlaybackState playbackState;
            public AtomicSafetyHandle ecbSafetyHandle; // used for temporary NativeArray views created & destroyed during playback
            public SystemHandle originSystem;
            private byte trackStructuralChanges;

            public void Init(EntityDataAccess* entityDataAccess, EntityCommandBufferData* data, in SystemHandle originSystemHandle)
            {
                mgr = entityDataAccess;
                originSystem = originSystemHandle;

                // Don't begin/end structural changes unless at least one command was recorded.
                // This prevents empty command buffers from needlessly causing structural changes, which some
                // existing code relies on.
                if (data->m_RecordedChainCount > 0)
                {
                    archetypeChanges = mgr->BeginStructuralChanges();
                    trackStructuralChanges = 1;
                }

                playbackState = new ECBSharedPlaybackState
                {
                    CommandBufferID = data->m_CommandBufferID,
                };
            }

            public void Cleanup()
            {
                if (trackStructuralChanges != 0)
                {
                    mgr->EndStructuralChanges(ref archetypeChanges);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyEntity(BasicCommand* header)
            {
                var cmd = (EntityCommand*)header;
                if (Hint.Unlikely(cmd->Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.DestroyEntity");
                var entity = cmd->Entity;
                mgr->DestroyEntityInternalDuringStructuralChange(&entity, 1, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.RemoveComponent");
                var entity = cmd->Header.Entity;
                mgr->RemoveComponentDuringStructuralChange(entity, ComponentType.FromTypeIndex(cmd->ComponentTypeIndex), in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponents(BasicCommand* header)
            {
                var cmd = (EntityMultipleComponentsCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.RemoveMultipleComponents");
                var entity = cmd->Header.Entity;
                var componentTypes = cmd->TypeSet;

                mgr->RemoveComponentDuringStructuralChange(entity, componentTypes, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AssignCachedChunkToExistingEntities(ECBPlaybackState* ecbPlaybackState, EntityArchetype entityArchetype, Entity* entities, int count)
            {
                Archetype* archetype = entityArchetype.Archetype;
                int chunkCapacity = archetype->ChunkCapacity;

                while (count != 0)
                {
                    //Check if the chunk is
                    ChunkIndex currentChunk = ecbPlaybackState->CurrentChunk;
                    if (currentChunk == ChunkIndex.Null
                        || ecbPlaybackState->CurrentChunkArchetype != archetype
                        || mgr->EntityComponentStore->GetArchetype(currentChunk) != archetype
                        || currentChunk.Count == archetype->ChunkCapacity)
                    {
                        currentChunk = mgr->GetChunkWithEmptySlots(entityArchetype);
                        ecbPlaybackState->CurrentChunk = currentChunk;
                        ecbPlaybackState->CurrentChunkArchetype = archetype;
                    }

                    var allocateCount = count < chunkCapacity - currentChunk.Count ? count : chunkCapacity - currentChunk.Count;
                    ChunkDataUtility.AssignChunkToExistingEntities(archetype, currentChunk, entities, allocateCount);
                    count -= allocateCount;
                    entities += allocateCount;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void CreateEntity(ECBPlaybackState* ecbPlaybackState, BasicCommand* header)
            {
                var cmd = (CreateCommand*)header;
                EntityArchetype entityArchetype = cmd->Archetype;

                if (!entityArchetype.Valid)
                    entityArchetype = mgr->GetEntityAndSimulateArchetype();

                if (StructuralChangesProfiler.Enabled)
                    mgr->StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in mgr->m_WorldUnmanaged);
                Entity* createdEntities = cmd->Entities;

                AssignCachedChunkToExistingEntities(ecbPlaybackState, entityArchetype, createdEntities, cmd->EntityCount);

                if (StructuralChangesProfiler.Enabled)
                    mgr->StructuralChangesRecorder.End();

                // The world owns these ids now. Null the recorded payload so a
                // dispose after a mid-playback exception releases only ids whose
                // commands never ran; releasing a consumed id here would recycle
                // it out from under a live entity.
                UnsafeUtility.MemClear(createdEntities, cmd->EntityCount * sizeof(Entity));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void InstantiateEntity(BasicCommand* header)
            {
                var cmd = (EntityCommand*)header;
                var srcEntity = cmd->Entity;
                var count = cmd->EntityCount;
                if (Hint.Unlikely(cmd->Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.InstantiateEntity");
                Entity* entities = cmd->Entities;

                mgr->InstantiateExistingInternalDuringStructuralChange(srcEntity, entities,
                    count, in originSystem);

                // As in CreateEntity: consumed ids must not be released by a
                // dispose after a mid-playback exception.
                UnsafeUtility.MemClear(entities, count * sizeof(Entity));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SetComponentFromEmbeddedValue(EntityComponentCommand* cmd, Entity entity)
            {
                byte* srcValue = (byte*)(cmd + 1);
                mgr->SetComponentDataRaw(entity, cmd->ComponentTypeIndex, srcValue, cmd->ComponentSize, originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddComponent");
                var entity = cmd->Header.Entity;
                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                if (cmd->ComponentSize != 0)
                    mgr->AddComponentDuringStructuralChange(entity, componentType, cmd + 1, cmd->ComponentSize, in originSystem);
                else
                    mgr->AddComponentDuringStructuralChange(entity, componentType, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponents(BasicCommand* header)
            {
                var cmd = (EntityMultipleComponentsCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddMultipleComponents");
                var entity = cmd->Header.Entity;
                var componentTypes = cmd->TypeSet;
                mgr->AddMultipleComponentsDuringStructuralChange(entity, componentTypes);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponent(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.SetComponent");
                var entity = cmd->Header.Entity;
                SetComponentFromEmbeddedValue(cmd, entity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetEnabled(BasicCommand* header)
            {
                var cmd = (EntityEnabledCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.SetEnabled");
                var entity = cmd->Header.Entity;
                mgr->SetEnabled(entity, cmd->IsEnabled != 0);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetComponentEnabled(BasicCommand* header)
            {
                var cmd = (EntityComponentEnabledCommand*)header;
                if (Hint.Unlikely(cmd->Header.Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.SetComponentEnabled");
                var entity = cmd->Header.Header.Entity;
                mgr->SetComponentEnabled(entity, cmd->ComponentTypeIndex, cmd->Header.IsEnabled != 0);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetName(BasicCommand* header)
            {
                var cmd = (EntityNameCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.SetName");
                var entity = cmd->Header.Entity;
                mgr->SetName(entity, in cmd->Name);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddBuffer(BasicCommand* header)
            {
                var cmd = (EntityBufferCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddBuffer");
                var entity = cmd->Header.Entity;
                mgr->AddComponentDuringStructuralChange(entity, ComponentType.FromTypeIndex(cmd->ComponentTypeIndex),
                    &cmd->BufferNode.TempBuffer, cmd->ComponentSize, in originSystem);

                // Clear the buffer header to mark that ownership has been transferred to the entity.
                // This avoids double disposal if the playback is interrupted by an exception.
                BufferHeader.Initialize(&cmd->BufferNode.TempBuffer, 0);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetBuffer(BasicCommand* header)
            {
                var cmd = (EntityBufferCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.SetBuffer");
                var entity = cmd->Header.Entity;
                mgr->SetBufferRaw(entity, cmd->ComponentTypeIndex, &cmd->BufferNode.TempBuffer,
                    cmd->ComponentSize, in originSystem);

                // Clear the buffer header to mark that ownership has been transferred to the entity.
                // This avoids double disposal if the playback is interrupted by an exception.
                BufferHeader.Initialize(&cmd->BufferNode.TempBuffer, 0);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AppendToBuffer(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AppendToBuffer");
                var entity = cmd->Header.Entity;
                CheckBufferExistsOnEntity(mgr->EntityComponentStore, entity, cmd);
                BufferHeader* bufferHeader =
                    (BufferHeader*)mgr->GetComponentDataRW_AsBytePointer(entity, cmd->ComponentTypeIndex);

                ref readonly var typeInfo = ref TypeManager.GetTypeInfo(cmd->ComponentTypeIndex);
                var alignment = typeInfo.AlignmentInBytes;
                var elementSize = typeInfo.ElementSize;

                BufferHeader.EnsureCapacity(bufferHeader, bufferHeader->Length + 1, elementSize, alignment, BufferHeader.TrashMode.RetainOldData, false, 0);

                var offset = bufferHeader->Length * elementSize;
                byte* dst = BufferHeader.GetElementPointer(bufferHeader) + offset;
                UnsafeUtility.MemCpy(dst, cmd + 1, (long)elementSize);
                bufferHeader->Length += 1;
            }

            // Creates a temporary NativeArray<Entity> view data from an Entity* pointer+count. This array does not need to be Disposed();
            // it does not own any of its data. The array will share a safety handle with the ECB, and its lifetime must not exceed that
            // of the ECB itself.
            private void CreateTemporaryNativeArrayView(Entity* entities, int entityCount,
                out NativeArray<Entity> outArray)
            {
                outArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Entity>(entities, entityCount,
                    Allocator.None);
                // The temporary array still needs an atomic handle, but the array itself will not have Dispose() called
                // on it (it doesn't own its data). And even if it did, NativeArray.Dispose() skips disposing the safety
                // handle if the array uses Allocator.None. The solution is to pass the primary ECB safety handle into
                // the playback processor, and use it for temporary arrays created during playback.
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref outArray, ecbSafetyHandle);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommand*)header;
                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary).
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                mgr->AddComponentToQueryDuringStructuralChange(cmd->Header.QueryImpl, componentType, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddComponentForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesComponentCommand*)header;
                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);

                CreateTemporaryNativeArrayView(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, out var entities);

                if (cmd->ComponentSize > 0)
                    mgr->AddComponentDuringStructuralChange(entities, componentType, cmd + 1, cmd->ComponentSize, in originSystem);
                else
                    mgr->AddComponentDuringStructuralChange(entities, componentType, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponentForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommand*)header;
                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);
                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary).
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                mgr->RemoveComponentFromQueryDuringStructuralChange(cmd->Header.QueryImpl, componentType, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveComponentForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesComponentCommand*)header;
                var componentType = ComponentType.FromTypeIndex(cmd->ComponentTypeIndex);

                CreateTemporaryNativeArrayView(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, out var entities);


                mgr->RemoveComponentDuringStructuralChange(entities, componentType, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesAndComponentsCommand*)header;

                CreateTemporaryNativeArrayView(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, out var entities);


                mgr->AddMultipleComponentsDuringStructuralChange(entities, cmd->TypeSet, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentTypeSetCommand*)header;
                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary).
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                mgr->AddComponentsToQueryDuringStructuralChange(cmd->Header.QueryImpl, cmd->TypeSet, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponentsForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesAndComponentsCommand*)header;

                CreateTemporaryNativeArrayView(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, out var entities);


                mgr->RemoveMultipleComponentsDuringStructuralChange(entities, cmd->TypeSet, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void RemoveMultipleComponentsForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentTypeSetCommand*)header;
                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary).
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                mgr->RemoveMultipleComponentsFromQueryDuringStructuralChange(cmd->Header.QueryImpl, cmd->TypeSet, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand*)header;

                CreateTemporaryNativeArrayView(cmd->Entities.Ptr, cmd->EntitiesCount, out var entities);


                mgr->DestroyEntityDuringStructuralChange(entities, in originSystem);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DestroyForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryCommand*)header;
                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary). And then immediately begin
                    // a new batch, to track the changes made by this operation.
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                mgr->DestroyEntitiesInQueryDuringStructuralChange(cmd->QueryImpl, in originSystem);
            }

            public void AddComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityQueryMaskCommand*) header;
                if (Hint.Unlikely(cmd->Header.Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddComponentLinkedEntityGroup");
                var entity = cmd->Header.Header.Entity;
                byte *srcValue = (byte*) (cmd + 1);
                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary). And then immediately begin
                    // a new batch, to track the changes made by this operation.
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                mgr->AddComponentForLinkedEntityGroup(entity, cmd->Mask, cmd->Header.ComponentTypeIndex, srcValue,
                    cmd->Header.ComponentSize);
            }

            public void SetComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityQueryMaskCommand*) header;
                if (Hint.Unlikely(cmd->Header.Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.SetComponentLinkedEntityGroup");
                var entity = cmd->Header.Header.Entity;
                byte *srcValue = (byte*) (cmd + 1);
                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary). And then immediately begin
                    // a new batch, to track the changes made by this operation.
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                mgr->SetComponentForLinkedEntityGroup(entity, cmd->Mask, cmd->Header.ComponentTypeIndex, srcValue,
                    cmd->Header.ComponentSize);
            }

            public void ReplaceComponentLinkedEntityGroup(BasicCommand* header)
            {
                var cmd = (EntityComponentCommand*) header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.ReplaceComponentLinkedEntityGroup");
                var entity = cmd->Header.Entity;
                byte *srcValue = (byte*) (cmd + 1);
                mgr->ReplaceComponentForLinkedEntityGroup(entity, cmd->ComponentTypeIndex, srcValue,
                    cmd->ComponentSize);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentData(BasicCommand* header)
            {
                var cmd = (EntityUnmanagedSharedComponentCommand*)header;
                if (Hint.Unlikely(cmd->Header.Entity == Entity.Null))
                    throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddUnmanagedSharedComponentData");
                var entity = cmd->Header.Entity;
                byte *srcValue = (byte*) (cmd + 1);
                var tmp = new NativeArray<Entity>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                tmp[0] = entity;
                mgr->AddSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(
                    tmp,
                    cmd->ComponentTypeIndex,
                    cmd->HashCode,
                    cmd->IsDefault == 0 ? (void*)srcValue : null);
                CommitStructuralChanges(mgr, ref archetypeChanges);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentData(BasicCommand* header)
            {
                var cmd = (EntityUnmanagedSharedComponentCommand*) header;
                var entity = cmd->Header.Entity;
                byte *srcValue = (byte*) (cmd + 1);
                var tmp = new NativeArray<Entity>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                tmp[0] = entity;
                mgr->SetSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(
                    tmp,
                    cmd->ComponentTypeIndex,
                    cmd->HashCode,
                    (cmd->IsDefault == 0) ? srcValue : null);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*)header;

                CreateTemporaryNativeArrayView(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, out var entities);


                var hashcode = cmd->HashCode;
                var typeIndex = cmd->ComponentTypeIndex;
                byte *srcValue = (byte*) (cmd + 1);

                mgr->AddSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(
                    entities,
                    typeIndex,
                    hashcode,
                    (cmd->IsDefault == 0) ? srcValue : null);

                CommitStructuralChanges(mgr, ref archetypeChanges);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void AddUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommandWithUnmanagedSharedComponent*)header;

                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary). And then immediately begin
                    // a new batch, to track the changes made by this operation.
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                var typeIndex = cmd->Header.ComponentTypeIndex;
                byte *srcValue = (byte*) (cmd + 1);

                int newSharedComponentDataIndex = mgr->InsertSharedComponent_Unmanaged(typeIndex, cmd->HashCode,
                    cmd->IsDefault == 1 ? null : srcValue,
                    null);
                mgr->AddSharedComponentDataToQueryDuringStructuralChange_Unmanaged(cmd->Header.Header.QueryImpl,
                    newSharedComponentDataIndex, ComponentType.FromTypeIndex(typeIndex),
                    cmd->IsDefault == 1 ? null : srcValue,
                    in originSystem);

                CommitStructuralChanges(mgr, ref archetypeChanges);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentValueForMultipleEntities(BasicCommand* header)
            {
                var cmd = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*)header;

                CreateTemporaryNativeArrayView(cmd->Header.Entities.Ptr, cmd->Header.EntitiesCount, out var entities);


                byte *srcValue = (byte*) (cmd + 1);

                mgr->SetSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(
                    entities,
                    cmd->ComponentTypeIndex,
                    cmd->HashCode,
                    cmd->IsDefault == 1 ? null : srcValue);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetUnmanagedSharedComponentValueForEntityQuery(BasicCommand* header)
            {
                var cmd = (EntityQueryComponentCommandWithUnmanagedSharedComponent*)header;

                if (Hint.Likely(trackStructuralChanges != 0))
                {
                    // We need to make sure any outstanding structural changes are applied before evaluating the query, to
                    // ensure that its matching chunk cache get invalidated (if necessary). And then immediately begin
                    // a new batch, to track the changes made by this operation.
                    CommitStructuralChanges(mgr, ref archetypeChanges);
                }
                var typeIndex = cmd->Header.ComponentTypeIndex;
                byte *srcValue = (byte*) (cmd + 1);

                int newSharedComponentDataIndex = mgr->InsertSharedComponent_Unmanaged(typeIndex, cmd->HashCode,
                    cmd->IsDefault == 1 ? null : srcValue,
                    null);
                mgr->SetSharedComponentDataOnQueryDuringStructuralChange_Unmanaged(cmd->Header.Header.QueryImpl,
                    newSharedComponentDataIndex, ComponentType.FromTypeIndex(typeIndex),
                    cmd->IsDefault == 1 ? null : srcValue,
                    in originSystem);

                CommitStructuralChanges(mgr, ref archetypeChanges);
            }

            public ECBProcessorType ProcessorType => ECBProcessorType.PlaybackProcessor;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckEntityNotNull(Entity entity)
        {
            if (entity == Entity.Null)
                throw new InvalidOperationException("Invalid Entity.Null passed.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckEntityVersionValid(Entity entity)
        {
            if (entity.Version <= 0)
                throw new InvalidOperationException("Invalid Entity version");
        }
        private static void CommitStructuralChanges(EntityDataAccess* mgr,
            ref EntityComponentStore.ArchetypeChanges archetypeChanges)
        {
            mgr->EndStructuralChanges(ref archetypeChanges);
            archetypeChanges = mgr->BeginStructuralChanges();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckBufferExistsOnEntity(EntityComponentStore* mgr, Entity entity, EntityComponentCommand* cmd)
        {
            if (!mgr->HasComponent(entity, cmd->ComponentTypeIndex, out bool entityExists))
                throw new InvalidOperationException($"Buffer does not exist on entity {entity} (entityExists:{entityExists}), cannot append element.");
        }
    }
}
