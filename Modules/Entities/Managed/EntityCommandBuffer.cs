// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    /// <summary>
    /// Specifies if the <see cref="EntityCommandBuffer"/> can be played a single time or multiple times.
    /// </summary>
    [Obsolete("PlaybackPolicy is obsolete. This will be removed in a future version and MultiPlayback will not be available.", true)]
    public enum PlaybackPolicy
    {
        /// <summary>
        /// The <see cref="EntityCommandBuffer"/> can only be played once. After a first playback, the EntityCommandBuffer must be disposed.
        /// </summary>
        SinglePlayback,
        /// <summary>
        /// The <see cref="EntityCommandBuffer"/> can be played back more than once.
        /// </summary>
        /// <remarks>Even though the EntityCommandBuffer can be played back more than once, no commands can be added after the first playback.</remarks>
        MultiPlayback
    }

    /// <summary>
    /// Specifies when an <see cref="EntityQuery"/> passed to an <see cref="EntityCommandBuffer"/> should be evaluated.
    /// </summary>
    /// <remarks>
    /// This can significantly affect which entities are matched by the query, as well as the overall performance of the
    /// requested operation.
    /// </remarks>
    public enum EntityQueryCaptureMode
    {
        /// <summary>
        /// (Obsolete) Request that the query's results be captured immediately, when the command is recorded.
        /// </summary>
        /// <remarks>
        /// The entire array of matching entities will be serialized into the command buffer, and this exact set of
        /// entities will be processed at playback time. This approach is far less efficient, but may lead to a more
        /// predictable set of entities being processed.
        ///
        /// At playback time, the command throws an error if one of these entities is destroyed before playback. (With
        /// safety checks enabled, an exception is thrown. Without safety checks, playback will perform invalid and
        /// unsafe memory access.)
        /// </remarks>
        [Obsolete("The Capture-query-at-record feature will be removed in a future version of the Entities package. Use AtPlayback mode instead. If AtRecord semantics are required, use the query to generate a NativeArray of matching entities to process, and pass the array instead of the query. (RemovedAfter Entities 2.0)")]
        AtRecord,

        /// <summary>
        /// Request that the query's results be captured when the corresponding command is played back.
        /// </summary>
        /// <remarks>
        /// Only a reference to the query itself is serialized into the command buffer. The requested operation is applied
        /// to the query during playback. This approach is generally far more efficient, but may lead to unexpected
        /// entities being processed (if entities which match the query are created or destroyed between recording the
        /// command buffer and playing it back).
        ///
        /// Since the serialized query is stored by reference, modifying or deleting the query after the
        /// command is recorded may affect the set of chunks and entities matched at playback time.
        /// </remarks>
        AtPlayback,
    }

    /// <summary>
    /// This attribute should be added to a public method in the `EntityCommandBuffer` class iff the following conditions are fulfilled:
    /// 1. The method is allowed to run inside of the Entities.ForEach() lambda function (one exception would be Playback(), since we do not
    /// want entity command buffers to be played back inside of Entities.ForEach());
    /// 2. Source-generation of the method when used inside Entities.ForEach() has been implemented.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class SupportedInEntitiesForEach : Attribute
    {
    }

    /// <summary>
    ///     A thread-safe command buffer that can buffer commands that affect entities and components for later playback.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [BurstCompile]
    public unsafe partial struct EntityCommandBuffer : IDisposable
    {
        /// <summary>
        ///     The minimum chunk size to allocate from the job allocator.
        /// </summary>
        /// We keep this relatively small as we don't want to overload the temp allocator in case people make a ton of command buffers.
        private const int kDefaultMinimumChunkSize = 4 * 1024;

        [NativeDisableUnsafePtrRestriction] internal EntityCommandBufferData* m_Data;

        internal int SystemID;
        internal SystemHandle OriginSystemHandle;
        internal int PassedPrePlaybackValidation; // non-zero if pre-playback validation ran on this ECB successfully; zero if it failed or didn't run at all.

        private struct ECBIDAllocator
        {
            public static readonly SharedStatic<int> Ref = SharedStatic<int>.GetOrCreate<EntityCommandBuffer, ECBIDAllocator>();
        }

        static readonly SharedStatic<int> _ms_CommandBufferIDAllocator = ECBIDAllocator.Ref;

        internal static int ms_CommandBufferIDAllocator
        {
            get => _ms_CommandBufferIDAllocator.Data;
            set => _ms_CommandBufferIDAllocator.Data = value;
        }

        private AtomicSafetyHandle m_Safety0;
        private AtomicSafetyHandle m_BufferSafety;
        private AtomicSafetyHandle m_ArrayInvalidationSafety;
        private int m_SafetyReadOnlyCount;
        private int m_SafetyReadWriteCount;

        internal void WaitForWriterJobs()
        {
            AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_Safety0);
            AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_BufferSafety);
            AtomicSafetyHandle.EnforceAllBufferJobsHaveCompleted(m_ArrayInvalidationSafety);
        }

        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<EntityCommandBuffer>();



        /// <summary>
        ///     Allows controlling the size of chunks allocated from the temp job allocator to back the command buffer.
        /// </summary>
        /// Larger sizes are more efficient, but create more waste in the allocator.
        public int MinimumChunkSize
        {
            get { return m_Data->m_MinimumChunkSize > 0 ? m_Data->m_MinimumChunkSize : kDefaultMinimumChunkSize; }
            set { m_Data->m_MinimumChunkSize = Math.Max(0, value); }
        }

        /// <summary>
        /// Returns true if the <see cref="EntityCommandBuffer"/> has not been initialized or no commands have been recorded.
        /// </summary>
        public bool IsEmpty => (m_Data == null) ? true : m_Data->m_RecordedChainCount == 0;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void EnforceSingleThreadOwnership()
        {
            if (m_Data == null)
                throw new NullReferenceException("The EntityCommandBuffer has not been initialized! The EntityCommandBuffer needs to be passed an Allocator when created!");
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety0);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void AssertDidNotPlayback()
        {
            if (m_Data != null && m_Data->m_DidPlayback)
                throw new InvalidOperationException("The EntityCommandBuffer has already been played back and no further commands can be added.");
        }

        /// <summary>
        ///  Creates a new command buffer.
        /// </summary>
        /// <param name="allocator">Memory allocator to use for chunks and data</param>
        public EntityCommandBuffer(AllocatorManager.AllocatorHandle allocator)
        {
            m_Data = (EntityCommandBufferData*)Memory.Unmanaged.Allocate(sizeof(EntityCommandBufferData), UnsafeUtility.AlignOf<EntityCommandBufferData>(), allocator);
            m_Data->m_Allocator = allocator;
            m_Data->m_MinimumChunkSize = kDefaultMinimumChunkSize;
            m_Data->m_ShouldPlayback = true;
            m_Data->m_DidPlayback = false;
            m_Data->m_CommandBufferID = --ms_CommandBufferIDAllocator;

            m_Data->m_MainThreadChain = default; // initial chains are lazily initialized when the first command is recorded

            m_Data->m_ThreadedChains = null;
            m_Data->m_RecordedChainCount = 0;

            SystemID = 0;
            PassedPrePlaybackValidation = 0;
            OriginSystemHandle = default;

            m_Safety0 = CollectionHelper.CreateSafetyHandle(allocator);

            // Used for all buffers returned from the API, so we can invalidate them once Playback() has been called.
            m_BufferSafety = AtomicSafetyHandle.Create();
            // Used to invalidate array aliases to buffers
            m_ArrayInvalidationSafety = AtomicSafetyHandle.Create();

            allocator.AddSafetyHandle(m_Safety0); // so that when allocator rewinds, this handle will invalidate
            allocator.AddSafetyHandle(m_BufferSafety); // so that when allocator rewinds, this handle will invalidate
            allocator.AddSafetyHandle(m_ArrayInvalidationSafety); // so that when allocator rewinds, this handle will invalidate

            m_SafetyReadOnlyCount = 0;
            m_SafetyReadWriteCount = 3;

            CollectionHelper.SetStaticSafetyId(ref m_Safety0, ref s_staticSafetyId.Data, "Unity.Entities.EntityCommandBuffer");
            AtomicSafetyHandle.SetStaticSafetyId(ref m_BufferSafety, s_staticSafetyId.Data); // uses id created above
            AtomicSafetyHandle.SetStaticSafetyId(ref m_ArrayInvalidationSafety, s_staticSafetyId.Data); // uses id created above
        }

        /// <summary>
        ///  Creates a new command buffer.
        /// </summary>
        /// <param name="label">Memory allocator to use for chunks and data</param>
        /// <param name="playbackPolicy">Specifies if the EntityCommandBuffer can be played a single time or more than once.</param>
        [Obsolete("Creating an ECB with a PlaybackPolicy is obsolete. This will be removed in a future version.", true)]
        public EntityCommandBuffer(Allocator label, PlaybackPolicy playbackPolicy)
        : this((AllocatorManager.AllocatorHandle)label)
        {
        }

        /// <summary>
        ///  Creates a new command buffer.
        /// </summary>
        /// <param name="allocator">Memory allocator to use for chunks and data</param>
        /// <param name="playbackPolicy">Specifies if the EntityCommandBuffer can be played a single time or more than once.</param>
        [Obsolete("Creating an ECB with a PlaybackPolicy is obsolete. This will be removed in a future version.", true)]
        public EntityCommandBuffer(AllocatorManager.AllocatorHandle allocator, PlaybackPolicy playbackPolicy)
        : this(allocator)
        {
        }

        /// <summary>
        /// Is true if the <see cref="EntityCommandBuffer"/> has been initialized correctly.
        /// </summary>
        public bool IsCreated   { get { return m_Data != null; } }

        [BurstCompile]
        static void DisposeInternal(ref EntityCommandBuffer ecb)
        {
            if (!ecb.IsCreated)
                return;
            k_ProfileEcbDispose.Begin();
            CollectionHelper.DisposeSafetyHandle(ref ecb.m_Safety0);
            AtomicSafetyHandle.Release(ecb.m_ArrayInvalidationSafety);
            AtomicSafetyHandle.Release(ecb.m_BufferSafety);

            // There's no need to walk chains and dispose individual allocations if the provided allocator
            // uses auto-dispose; they'll all be freed automatically when the allocator rewinds.
            bool disposeChains = !ecb.m_Data->m_Allocator.IsAutoDispose;
            // ...however, under some conditions we need to walk the chains anyway and manually free their allocations,
            // even with auto-dispose allocators.
            if (ecb.m_Data->m_ForceFullDispose && !ecb.m_Data->m_DidPlayback)
            {
                disposeChains = true;
            }
            if (ecb.m_Data != null && !ecb.m_Data->m_DidPlayback)
            {
                DeallocateRecordedEntities(ecb.m_Data);
            }

            if (ecb.m_Data != null && disposeChains)
            {
                ecb.FreeChain(&ecb.m_Data->m_MainThreadChain, ecb.m_Data->m_DidPlayback);
                if (ecb.m_Data->m_ThreadedChains != null)
                {
                    int maxThreadCount = JobsUtility.ThreadIndexCount;
                    for (int i = 0; i < maxThreadCount; ++i)
                    {
                        ecb.FreeChain(&ecb.m_Data->m_ThreadedChains[i], ecb.m_Data->m_DidPlayback);
                    }

                    ecb.m_Data->DestroyForParallelWriter();
                }

                Memory.Unmanaged.Free(ecb.m_Data, ecb.m_Data->m_Allocator);
            }
            ecb.m_Data = null;
            k_ProfileEcbDispose.End();
        }

        // Batch size for DeallocateRecordedEntities. 512 entities = 4 KB on the stack —
        // bounded peak memory regardless of how many entities a non-played-back ECB recorded.
        private const int k_DeallocateBatchSize = 512;

        private static void FlushRecordedEntitiesFromChain(
            EntityCommandBufferChain* chain, Entity* batch, ref int filled)
        {
            for (var chunk = chain->m_Head; chunk != null; chunk = chunk->Next)
            {
                var buf = (byte*)chunk + sizeof(ECBChunk);
                int offset = 0;
                while (offset < chunk->Used)
                {
                    var header = (BasicCommand*)(buf + offset);
                    var commandType = (ECBCommand)header->CommandType;
                    Entity* src = null;
                    int n = 0;
                    if (commandType == ECBCommand.CreateEntity)
                    {
                        var cmd = (CreateCommand*)header;
                        src = cmd->Entities;
                        n = cmd->EntityCount;
                    }
                    else if (commandType == ECBCommand.InstantiateEntity)
                    {
                        var cmd = (EntityCommand*)header;
                        src = cmd->Entities;
                        n = cmd->EntityCount;
                    }

                    while (n > 0)
                    {
                        int remaining = k_DeallocateBatchSize - filled;
                        int take = n < remaining ? n : remaining;
                        UnsafeUtility.MemCpy(batch + filled, src, take * sizeof(Entity));
                        filled += take;
                        src += take;
                        n -= take;
                        if (filled == k_DeallocateBatchSize)
                        {
                            EntityComponentStore.s_entityStore.Data.DeallocateEntities(batch, filled);
                            filled = 0;
                        }
                    }
                    offset += header->TotalSize;
                }
            }
        }

        private static void DeallocateRecordedEntities(EntityCommandBufferData* data)
        {
            var batch = stackalloc Entity[k_DeallocateBatchSize];
            int filled = 0;

            for (var chain = &data->m_MainThreadChain; chain != null; chain = chain->m_NextChain)
                FlushRecordedEntitiesFromChain(chain, batch, ref filled);

            if (data->m_ThreadedChains != null)
            {
                int maxThreadCount = JobsUtility.ThreadIndexCount;
                for (int i = 0; i < maxThreadCount; ++i)
                    for (var chain = &data->m_ThreadedChains[i]; chain != null; chain = chain->m_NextChain)
                        FlushRecordedEntitiesFromChain(chain, batch, ref filled);
            }

            if (filled > 0)
                EntityComponentStore.s_entityStore.Data.DeallocateEntities(batch, filled);
        }

        /// <summary>
        /// Deals with freeing and releasing unmanaged memory allocated by the entity command buffer.
        /// </summary>
        public void Dispose()
        {
            DisposeInternal(ref this); // forward to Burst-compiled function
        }
        private void FreeChain(EntityCommandBufferChain* chain, bool didPlayback)
        {
            if (chain->m_Head == null)
                return; // skip uninitialized chains;
            bool first = true;
            while (chain != null)
            {
                // Buffers whose ownership wasn't transferred to an entity during playback must be freed here.
                if (!didPlayback)
                {
                    var bufferCleanupList = chain->m_Cleanup->BufferCleanupList;
                    while (bufferCleanupList != null)
                    {
                        var prev = bufferCleanupList->Prev;
                        BufferHeader.Destroy(&bufferCleanupList->TempBuffer);
                        bufferCleanupList = prev;
                    }
                }
                chain->m_Cleanup->BufferCleanupList = null;

                // Arrays of entities captured from an input EntityQuery at record time are always cleaned up
                // at Dispose time.
                var entityArraysCleanupList = chain->m_Cleanup->EntityArraysCleanupList;
                while (entityArraysCleanupList != null)
                {
                    var prev = entityArraysCleanupList->Prev;
                    Memory.Unmanaged.Free(entityArraysCleanupList->Ptr, m_Data->m_Allocator);
                    entityArraysCleanupList = prev;
                }
                chain->m_Cleanup->EntityArraysCleanupList = null;
                Memory.Unmanaged.Free(chain->m_Cleanup, m_Data->m_Allocator);
                while (chain->m_Tail != null)
                {
                    var prev = chain->m_Tail->Prev;
                    Memory.Unmanaged.Free(chain->m_Tail, m_Data->m_Allocator);
                    chain->m_Tail = prev;
                }
                chain->m_Head = null;

                var chainToFree = chain;
                chain = chain->m_NextChain;
                chainToFree->m_NextChain = null;
                if (!first)
                {
                    // we need to free the chain we have just visited, but only if it is not the first one
                    Memory.Unmanaged.Free(chainToFree, m_Data->m_Allocator);
                }
                first = false;
            }
        }
        internal int MainThreadSortKey => Int32.MaxValue;

        /// <summary>Records a command to create an entity with specified archetype.</summary>
        /// <remarks>At playback, this command throws an error if the archetype contains the <see cref="Prefab"/> tag.</remarks>
        /// <param name="archetype">The archetype of the new entity.</param>
        /// <returns>An entity that will be assigned a chunk when this EntityCommandBuffer is played back.</returns>
        /// <exception cref="ArgumentException">Throws if the archetype is null.</exception>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public Entity CreateEntity(EntityArchetype archetype)
        {
            archetype.CheckValidEntityArchetype();
            Entity entity;
            _CreateEntity(archetype,&entity, 1);
            return entity;
        }

        /// <summary>Records a command to create an entity with no components.</summary>
        /// <returns>An entity that will be assigned a chunk when this EntityCommandBuffer is played back.</returns>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public Entity CreateEntity()
        {
            EntityArchetype archetype = new EntityArchetype();
            Entity entity;
            _CreateEntity(archetype,  &entity, 1);
            return entity;
        }

        /// <summary>Records a command to create an array of entities with specified archetype.</summary>
        /// <remarks>At playback, the entities will be assigned a chunk. Additionally, this command throws an error if the archetype contains the <see cref="Prefab"/> tag.</remarks>
        /// <param name="archetype">The archetype of the new entities.</param>
        /// <param name="entities">An array to hold the Entity objects needed to access the new entities.
        /// The length of the array determines how many entities are created.</param>
        /// <exception cref="ArgumentException">Throws if the archetype is null.</exception>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        public void CreateEntity(EntityArchetype archetype, NativeArray<Entity> entities)
        {
            archetype.CheckValidEntityArchetype();
            _CreateEntity(archetype, (Entity*)entities.GetUnsafePtr(), entities.Length);
        }

        /// <summary>Records a command to create a specified number of entities with no components.</summary>
        /// <param name="count">The number of entities to create.</param>
        /// <param name="allocator">The allocator to use for the returned array.</param>
        /// <returns>An array of the created entities. Each entity is valid immediately, but Unity doesn't assign it a chunk until the Playback method runs.</returns>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        public NativeArray<Entity> CreateEntity(int count, Allocator allocator)
        {
            EntityArchetype archetype = new EntityArchetype();
            NativeArray<Entity> entities = new NativeArray<Entity>(count, allocator);
            _CreateEntity(archetype, (Entity*)entities.GetUnsafePtr(), count);
            return entities;
        }

        private void _CreateEntity(EntityArchetype archetype, Entity* entities, int count)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            EntityComponentStore.s_entityStore.Data.AllocateEntities(entities, count);
            m_Data->AddCreateCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.CreateEntity, entities, count, archetype);
            m_Data->m_MainThreadChain.m_EntityCount += count;
        }

        /// <summary>Records a command to create an entity with specified entity prefab.</summary>
        /// <remarks>An instantiated entity has the same components and component values as the
        /// prefab entity, minus the Prefab tag component.
        /// If the source entity was destroyed before playback, this command throws an error.
        ///
        /// The returned entity is the root of the instantiated prefab. It is valid for use in
        /// subsequent commands on this buffer, but has no chunk until the ECB has been played back <see cref="Playback"/>.
        /// For a prefab with a <c>LinkedEntityGroup</c>, only the root is allocated at record
        /// time. The child entity IDs are only available via the root's <c>LinkedEntityGroup</c>
        /// buffer after playback.</remarks>
        /// <param name="e">The entity prefab.</param>
        /// <returns>The root entity of the instantiated prefab.</returns>
        /// <exception cref="ArgumentNullException"> Thrown if Entity e is null and if safety checks are enabled.</exception>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public Entity Instantiate(Entity e)
        {
            CheckEntityNotNull(e);
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            Entity entity;
            EntityComponentStore.s_entityStore.Data.AllocateEntities(&entity, 1);
            m_Data->AddEntityCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.InstantiateEntity,
                 entity, e);
            m_Data->m_MainThreadChain.m_EntityCount += 1;
            return entity;
        }

        /// <summary>Records a command to create a NativeArray of entities with specified entity prefab.</summary>
        /// <remarks>An instantiated entity has the same components and component values as the prefab entity, minus the Prefab tag component.
        /// If the source entity was destroyed before playback, this command throws an error.
        ///
        /// The filled entities are the roots of each instantiated prefab. They are valid for use
        /// in subsequent commands on this buffer, but have no chunk until the ECB has been played back <see cref="Playback"/>.
        /// For a prefab with a <c>LinkedEntityGroup</c>, only the roots are allocated at record
        /// time. The child entity IDs are only available via each root's <c>LinkedEntityGroup</c>
        /// buffer after playback.</remarks>
        /// <param name="e">The entity prefab.</param>
        /// <param name="entities">A NativeArray that receives the root entities of the instantiated prefabs.</param>
        /// <exception cref="ArgumentNullException"> Thrown if Entity e is null and if safety checks are enabled.</exception>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void Instantiate(Entity e, NativeArray<Entity> entities)
        {
            CheckEntityNotNull(e);
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            EntityComponentStore.s_entityStore.Data.AllocateEntities(entities);
            m_Data->AddMultipleEntityCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.InstantiateEntity, entities, e);
            m_Data->m_MainThreadChain.m_EntityCount += entities.Length;
        }

        /// <summary>Records a command to destroy an entity.</summary>
        /// <remarks>At playback, this command throws an error if the entity was destroyed between recording and playback,
        /// or if the entity has the <see cref="Prefab"/> tag.</remarks>
        /// <param name="e">The entity to destroy.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void DestroyEntity(Entity e)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.DestroyEntity, Entity.Null, e);
        }

        /// <summary>Records a command to destroy a NativeArray of entities.</summary>
        /// <remarks>At playback, this command only runs if the entity count is greater than 0.
        /// This command throws an error if any of the entities were destroyed between recording and playback,
        /// or if any of the entities have the <see cref="Prefab"/> tag.</remarks>
        /// <param name="entities">The NativeArray of entities to destroy.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void DestroyEntity(NativeArray<Entity> entities)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            m_Data->AppendMultipleEntitiesCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.DestroyMultipleEntities, entitiesCopy, entities.Length);
        }

        /// <summary>Records a command to add a dynamic buffer to an entity.</summary>
        /// <remarks>At playback, if the entity already has this type of dynamic buffer,
        /// this method sets the dynamic buffer contents. If the entity doesn't have a
        /// <see cref="DynamicBuffer{T}"/> component that stores elements of type T, then
        /// this method adds a DynamicBuffer component with the provided contents. If the
        /// entity is destroyed before playback, an error is thrown.</remarks>
        /// <param name="e">The entity to add the dynamic buffer to.</param>
        /// <typeparam name="T">The <see cref="IBufferElementData"/> type stored by the <see cref="DynamicBuffer{T}"/>.</typeparam>
        /// <returns>The <see cref="DynamicBuffer{T}"/> that will be added when the command plays back.</returns>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public DynamicBuffer<T> AddBuffer<T>(Entity e) where T : unmanaged, IBufferElementData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            return m_Data->CreateBufferCommand<T>(ECBCommand.AddBuffer, &m_Data->m_MainThreadChain, MainThreadSortKey, e, m_BufferSafety, m_ArrayInvalidationSafety);
        }

        /// <summary>Records a command to set a dynamic buffer on an entity.</summary>
        /// <remarks>At playback this command throws an error if the entity is destroyed
        /// before playback, or if the entity doesn't have a <see cref="DynamicBuffer{T}"/> component that
        /// stores elements of type T.</remarks>
        /// <param name="e">The entity to set the dynamic buffer on.</param>
        /// <typeparam name="T">The <see cref="IBufferElementData"/> type stored by the <see cref="DynamicBuffer{T}"/>.</typeparam>
        /// <returns>The <see cref="DynamicBuffer{T}"/> that will be set when the command plays back.</returns>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public DynamicBuffer<T> SetBuffer<T>(Entity e) where T : unmanaged, IBufferElementData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            return m_Data->CreateBufferCommand<T>(ECBCommand.SetBuffer, &m_Data->m_MainThreadChain, MainThreadSortKey, e, m_BufferSafety, m_ArrayInvalidationSafety);
        }

        /// <summary>Records a command to append a single element to the end of a dynamic buffer component.</summary>
        /// <remarks>At playback this command throws an error if the entity is destroyed
        /// before playback, or if the entity doesn't have a <see cref="DynamicBuffer{T}"/> component that
        /// stores elements of type T.</remarks>
        /// <param name="e">The entity to which the dynamic buffer belongs.</param>
        /// <param name="element">The new element to add to the <see cref="DynamicBuffer{T}"/> component.</param>
        /// <typeparam name="T">The <see cref="IBufferElementData"/> type stored by the <see cref="DynamicBuffer{T}"/>.</typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AppendToBuffer<T>(Entity e, T element) where T : struct, IBufferElementData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AppendToBufferCommand<T>(&m_Data->m_MainThreadChain, MainThreadSortKey, e, element);
        }

        /// <summary> Records a command to add component of type T to an entity. </summary>
        /// <remarks>At playback, if the entity already has this type of component, the value will just be set.
        /// Throws an error if this entity is destroyed before playback,
        /// if T is type Entity or <see cref="Prefab"/>, or adding this component type makes the archetype too large.</remarks>
        /// <param name="e"> The entity to have the component added. </param>
        /// <param name="component">The value to add on the new component in playback for the entity.</param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent<T>(Entity e, T component) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypeWithValueCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.AddComponent, e, component);
        }

        /// <summary> Records a command to add component of type T to a NativeArray of entities. </summary>
        /// <remarks>At playback, if any entity already has this type of component, the value will just be set.
        /// Throws an error if any entity is destroyed before playback,
        /// if T is type Entity or <see cref="Prefab"/>, or adding this component type makes the archetype too large.</remarks>
        /// <param name="entities"> The NativeArray of entities to have the component added. </param>
        /// <param name="component">The value to add on the new component in playback for all entities in the NativeArray.</param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent<T>(NativeArray<Entity> entities, T component) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            m_Data->AppendMultipleEntitiesComponentCommandWithValue(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.AddComponentForMultipleEntities, entitiesCopy, entities.Length, component);
        }

        /// <summary> Records a command to add component of type T to an entity. </summary>
        /// <remarks>At playback, this command does nothing if the entity already has the component.
        /// Throws an error if this entity is destroyed before playback,
        /// if T is type Entity or <see cref="Prefab"/>, or adding this component type makes the archetype too large.</remarks>
        /// <param name="e"> The entity to have the component added. </param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent<T>(Entity e) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypeWithoutValueCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.AddComponent, e, ComponentType.ReadWrite<T>());
        }

        /// <summary> Records a command to add component of type T to a NativeArray of entities. </summary>
        /// <remarks>At playback, if an entity already has this component, it is skipped.
        /// Throws an error if any entity is destroyed before playback,
        /// if T is type Entity or <see cref="Prefab"/>, or adding this component type makes the archetype too large.</remarks>
        /// <param name="entities"> The NativeArray of entities to have the component added. </param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent<T>(NativeArray<Entity> entities) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            m_Data->AppendMultipleEntitiesComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.AddComponentForMultipleEntities, entitiesCopy, entities.Length, ComponentType.ReadWrite<T>());
        }

        /// <summary> Records a command to add a component to an entity. </summary>
        /// <remarks>At playback, this command does nothing if the entity already has the component.
        /// Throws an error if any entity is destroyed before playback,
        /// if component is type Entity or <see cref="Prefab"/>, or adding this component type makes the archetype too large.</remarks>
        /// <param name="e"> The entity to get the additional component. </param>
        /// <param name="componentType"> The type of component to add. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent(Entity e, ComponentType componentType)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypeWithoutValueCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.AddComponent, e, componentType);
        }

        /// <summary> Records a command to add a component to an entity. </summary>
        /// <remarks>At playback, this command does nothing if the entity already has the component.
        /// Throws an error if any entity is destroyed before playback,
        /// if component is type Entity or <see cref="Prefab"/>, or adding this component type makes the archetype too large.</remarks>
        /// <param name="typeIndex"> The TypeIndex of the component being set. </param>
        /// <param name="typeSize"> The Size of the type of the component being set. </param>
        /// <param name="componentDataPtr"> The pointer to the data of the component to be copied. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        internal void UnsafeAddComponent(Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->UnsafeAddEntityComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.AddComponent, e, typeIndex, typeSize, componentDataPtr);
        }

        /// <summary> Records a command to add a component to a NativeArray of entities. </summary>
        /// <remarks>At playback, if an entity already has this component, it is skipped.
        /// Throws an error if any entity is destroyed before playback,
        /// if component is type Entity or <see cref="Prefab"/>, or adding this component type makes the archetype too large.</remarks>
        /// <param name="entities"> The NativeArray of entities to have the component added. </param>
        /// <param name="componentType"> The type of component to add. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent(NativeArray<Entity> entities, ComponentType componentType)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            m_Data->AppendMultipleEntitiesComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.AddComponentForMultipleEntities, entitiesCopy, entities.Length, componentType);
        }


        /// <summary> Records a command to add one or more components to an entity. </summary>
        /// <remarks>At playback, you can include a component type that the entity already has.
        /// Throws an error if this entity is destroyed before playback,
        /// if any component type is type Entity or <see cref="Prefab"/>, or adding a component type makes the archetype too large.</remarks>
        /// <param name="e"> The entity to get additional components. </param>
        /// <param name="componentTypeSet"> The types of components to add. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent(Entity e, in ComponentTypeSet componentTypeSet)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypesCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.AddMultipleComponents, e, componentTypeSet);
        }

        /// <summary> Records a command to add one or more components to a NativeArray of entities. </summary>
        /// <remarks>At playback, you can include a component type that any of the entities already have.
        /// Throws an error if this entity is destroyed before playback,
        /// if any component type is type Entity or <see cref="Prefab"/>, or adding a component type makes the archetype too large.</remarks>
        /// <param name="entities"> The NativeArray of entities to have the components added. </param>
        /// <param name="componentTypeSet"> The types of components to add. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent(NativeArray<Entity> entities, in ComponentTypeSet componentTypeSet)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            m_Data->AppendMultipleEntitiesMultipleComponentsCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.AddMultipleComponentsForMultipleEntities, entitiesCopy, entities.Length, componentTypeSet);
        }

        /// <summary> Records a command to set a component value on an entity.</summary>
        /// <remarks> At playback, this command throws an error if the entity is destroyed before playback,
        /// if the entity doesn't have the component type,
        /// if the entity has the <see cref="Prefab"/> tag, or if T is zero sized.</remarks>
        /// <param name="e"> The entity to set the component value of. </param>
        /// <param name="component"> The component value to set. </param>
        /// <typeparam name="T"> The type of component to set. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void SetComponent<T>(Entity e, T component) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypeWithValueCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.SetComponent, e, component);
        }

        /// <summary> Records a command to set a component value on an entity.</summary>
        /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
        /// if the entity doesn't have the component type,
        /// if the entity has the <see cref="Prefab"/> tag, or if T is zero sized.</remarks>
        /// <param name="e"> The entity to set the component value of. </param>
        /// <param name="typeIndex"> The TypeIndex of the component being set. </param>
        /// <param name="typeSize"> The Size of the type of the component being set. </param>
        /// <param name="componentDataPtr"> The pointer to the data of the component to be copied. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        internal void UnsafeSetComponent(Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->UnsafeAddEntityComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.SetComponent, e, typeIndex, typeSize, componentDataPtr);
        }

        /// <summary>
        /// Records a command to add or remove the <see cref="Disabled"/> component. By default EntityQuery does not include entities containing the Disabled component.
        /// Enabled entities are processed by systems, disabled entities are not.
        ///
        /// If the entity was converted from a prefab and thus has a <see cref="LinkedEntityGroup"/> component, the entire group will be enabled or disabled.
        /// </summary>
        /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
        /// if the entity has the <see cref="Prefab"/> tag.</remarks>
        /// <param name="e">The entity whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        [GenerateTestsForBurstCompatibility]
        public void SetEnabled(Entity e, bool value)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();

            m_Data->AddEntityEnabledCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.SetEntityEnabled, e, value);
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
        /// <param name="e">The entity whose component should be enabled or disabled.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleEnableableComponent) })]
        [SupportedInEntitiesForEach]
        public void SetComponentEnabled<T>(Entity e, bool value) where T : struct, IEnableableComponent
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentEnabledCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.SetComponentEnabled, e, TypeManager.GetTypeIndex<T>(), value);
        }
        /// <summary>
        /// Records a command to enable or disable a <see cref="ComponentType"/> on the specified <see cref="Entity"/>. This operation
        /// does not cause a structural change, or affect the value of the component. For the purposes
        /// of EntityQuery matching, an entity with a disabled component will behave as if it does not have that component.
        /// </summary>
        /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
        /// if the entity has the <see cref="Prefab"/> tag, or if the entity doesn't have the component type.</remarks>
        /// <param name="e">The entity whose component should be enabled or disabled.</param>
        /// <param name="componentType">The component type to enable or disable. This type must implement the
        /// <see cref="IEnableableComponent"/> interface.</param>
        /// <param name="value">True if the specified component should be enabled, or false if it should be disabled.</param>
        [SupportedInEntitiesForEach]
        public void SetComponentEnabled(Entity e, ComponentType componentType, bool value)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentEnabledCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.SetComponentEnabled, e, componentType.TypeIndex, value);
        }

        /// <summary> Records a command to set a name of an entity if Debug Names is enabled.</summary>
        /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
        /// if the entity has the <see cref="Prefab"/> tag, or if the EntityNameStore has reached its limit.</remarks>
        /// <param name="e"> The entity to set the name value of. </param>
        /// <param name="name"> The name to set. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void SetName(Entity e, in FixedString64Bytes name)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityNameCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.SetName, e, name);
        }

        /// <summary> Records a command to remove component of type T from an entity. </summary>
        /// <remarks> At playback, it's not an error if the entity doesn't have component T.
        /// Will throw an error if this entity is destroyed before playback,
        /// or if T is type Entity or <see cref="Prefab"/>.</remarks>
        /// <param name="e"> The entity to have the component removed. </param>
        /// <typeparam name="T"> The type of component to remove. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent<T>(Entity e)
        {
            RemoveComponent(e, ComponentType.ReadWrite<T>());
        }

        /// <summary> Records a command to remove component of type T from a NativeArray of entities. </summary>
        /// <remarks>At playback, it's not an error if any entity doesn't have component T.
        /// Will throw an error if one of these entities is destroyed before playback,
        /// or if T is type Entity or <see cref="Prefab"/>.</remarks>
        /// <param name="entities"> The NativeArray of entities to have the component removed. </param>
        /// <typeparam name="T"> The type of component to remove. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent<T>(NativeArray<Entity> entities)
        {
            RemoveComponent(entities, ComponentType.ReadWrite<T>());
        }

        /// <summary> Records a command to remove a component from an entity. </summary>
        /// <remarks>At playback, it's not an error if the entity doesn't have the component type.
        /// Will throw an error if this entity is destroyed before playback,
        /// or if the component type is Entity or <see cref="Prefab"/>.</remarks>
        /// <param name="e"> The entity to have the component removed. </param>
        /// <param name="componentType"> The type of component to remove. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent(Entity e, ComponentType componentType)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypeWithoutValueCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.RemoveComponent, e, componentType);
        }

        /// <summary> Records a command to remove one or more components from a NativeArray of entities. </summary>
        /// <remarks>At playback, it's not an error if any entity doesn't have the component type.
        /// Will throw an error if one of these entities is destroyed before playback,
        /// or if the component type is Entity or <see cref="Prefab"/>.</remarks>
        /// <param name="entities"> The NativeArray of entities to have the component removed. </param>
        /// <param name="componentType"> The type of component to remove. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent(NativeArray<Entity> entities, ComponentType componentType)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            m_Data->AppendMultipleEntitiesComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.RemoveComponentForMultipleEntities, entitiesCopy, entities.Length, componentType);
        }

        /// <summary> Records a command to remove one or more components from an entity. </summary>
        /// <remarks>At playback, it's not an error if the entity doesn't have one of the component types.
        /// Will throw an error if this entity is destroyed before playback,
        /// or if any of the component types are Entity or <see cref="Prefab"/>.</remarks>
        /// <param name="e"> The entity to have components removed. </param>
        /// <param name="componentTypeSet"> The types of components to remove. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent(Entity e, in ComponentTypeSet componentTypeSet)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypesCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.RemoveMultipleComponents, e, componentTypeSet);
        }

        /// <summary> Records a command to remove one or more components from a NativeArray of entities. </summary>
        /// <remarks>At playback, it's not an error if any entity doesn't have one of the component types.
        /// Will throw an error if one of these entities is destroyed before playback,
        /// or if any of the component types are Entity or <see cref="Prefab"/>.</remarks>
        /// <param name="entities"> The NativeArray of entities to have components removed. </param>
        /// <param name="componentTypeSet"> The types of components to remove. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent(NativeArray<Entity> entities, in ComponentTypeSet componentTypeSet)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            m_Data->AppendMultipleEntitiesMultipleComponentsCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.RemoveMultipleComponentsForMultipleEntities, entitiesCopy, entities.Length, componentTypeSet);
        }

        /// <summary>Records a command to add a component to all entities matching a query.</summary>
        /// <remarks>
        /// Does not affect entities which already have the component.
        /// </remarks>
        /// <param name="entityQuery">The query specifying the entities to which the component is added. </param>
        /// <param name="componentType">The type of component to add.</param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent(EntityQuery entityQuery, ComponentType componentType, EntityQueryCaptureMode queryCaptureMode)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            if (queryCaptureMode == EntityQueryCaptureMode.AtRecord)
#pragma warning restore 0618
                m_Data->AppendMultipleEntitiesComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.AddComponentForMultipleEntities, entityQuery, componentType);
            else
                m_Data->AppendEntityQueryComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.AddComponentForEntityQuery, entityQuery, componentType);
        }
        /// <summary>Obsolete. Use <see cref="AddComponent(EntityQuery,ComponentType,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <param name="componentType">The type of component to add.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void AddComponent(EntityQuery entityQuery, ComponentType componentType)
            => AddComponent(entityQuery, componentType, EntityQueryCaptureMode.AtRecord);
        /// <summary>Obsolete. Use <see cref="AddComponent(EntityQuery,ComponentType,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added. </param>
        /// <param name="componentType">The type of component to add.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("Use AddComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> AddComponent(*)")]
        public void AddComponentForEntityQuery(EntityQuery entityQuery, ComponentType componentType)
            => AddComponent(entityQuery, componentType, EntityQueryCaptureMode.AtRecord);

        /// <summary>Records a command to add a component to all entities matching a query.</summary>
        /// <remarks>
        /// Does not affect entities which already have the component.
        /// </remarks>
        /// <param name="entityQuery">The query specifying the entities to which the component is added. </param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent<T>(EntityQuery entityQuery, EntityQueryCaptureMode queryCaptureMode)
            => AddComponent(entityQuery, ComponentType.ReadWrite<T>(), queryCaptureMode);
        /// <summary>Obsolete. Use <see cref="AddComponent{T}(EntityQuery,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void AddComponent<T>(EntityQuery entityQuery)
            => AddComponent(entityQuery, ComponentType.ReadWrite<T>(), EntityQueryCaptureMode.AtRecord);
        /// <summary>Obsolete. Use <see cref="AddComponent{T}(EntityQuery,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added. </param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("Use AddComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> AddComponent<T>(*)")]
        public void AddComponentForEntityQuery<T>(EntityQuery entityQuery)
            => AddComponent(entityQuery, ComponentType.ReadWrite<T>(), EntityQueryCaptureMode.AtRecord);


        /// <summary>Records a command to add a component to all entities matching a query. Also sets the value of this new component on all the matching entities.</summary>
        /// <remarks>The set of entities matching the query is 'captured' in the method call, and the recorded command stores an array of all these entities.
        ///
        /// Entities which already have the component type will have the component set to the value.
        ///
        /// At playback, this command throws an error if one of these entities is destroyed before playback. (With safety checks enabled, an exception is thrown. Without safety checks,
        /// playback will perform invalid and unsafe memory access.)
        /// </remarks>
        /// <param name="entityQuery">The query specifying the entities to which the component is added. </param>
        /// <param name="value">The value to set on the new component in playback for all entities matching the query.</param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent<T>(EntityQuery entityQuery, T value) where T : unmanaged, IComponentData
        {
            // TODO(DOTS-8709): There is no efficient capture-at-playback path for this operation. Add one, or remove this method.
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AppendMultipleEntitiesComponentCommandWithValue(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.AddComponentForMultipleEntities, entityQuery, value);
        }
        /// <summary>Obsolete. Use <see cref="AddComponent{T}(EntityQuery, T)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added. </param>
        /// <param name="value">The value to set on the new component in playback for all entities matching the query.</param>
        /// <typeparam name="T"> The type of component to add. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("Use AddComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> AddComponent<T>(*)")]
        public void AddComponentForEntityQuery<T>(EntityQuery entityQuery, T value) where T : unmanaged, IComponentData
            => AddComponent<T>(entityQuery, value);

        /// <summary>Records a command to add multiple components to all entities matching a query.</summary>
        /// <remarks>The set of entities matching the query is 'captured' in the method call, and the recorded command stores an array of all these entities.
        ///
        /// Some matching entities may already have some or all of the specified components. After this operation, all matching entities will have all of the components.
        ///
        /// At playback, this command throws an error if one of these entities is destroyed before playback. (With safety checks enabled, an exception is thrown. Without safety checks,
        /// playback will perform invalid and unsafe memory access.)
        /// </remarks>
        /// <param name="entityQuery">The query specifying the entities to which the components are added. </param>
        /// <param name="componentTypeSet">The types of components to add.</param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddComponent(EntityQuery entityQuery, in ComponentTypeSet componentTypeSet, EntityQueryCaptureMode queryCaptureMode)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            if (queryCaptureMode == EntityQueryCaptureMode.AtRecord)
#pragma warning restore 0618
                m_Data->AppendMultipleEntitiesMultipleComponentsCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.AddMultipleComponentsForMultipleEntities, entityQuery, componentTypeSet);
            else
                m_Data->AppendEntityQueryComponentTypeSetCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.AddMultipleComponentsForEntityQuery, entityQuery, componentTypeSet);
        }
        /// <summary>Obsolete. Use <see cref="AddComponent(EntityQuery,in ComponentTypeSet,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <param name="componentTypeSet">The types of components to add.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void AddComponent(EntityQuery entityQuery, in ComponentTypeSet componentTypeSet)
            => AddComponent(entityQuery, componentTypeSet, EntityQueryCaptureMode.AtRecord);
        /// <summary>Obsolete. Use <see cref="AddComponent(EntityQuery,in ComponentTypeSet,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the components are added. </param>
        /// <param name="componentTypeSet">The types of components to add.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("Use AddComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> AddComponent(*)")]
        public void AddComponentForEntityQuery(EntityQuery entityQuery, in ComponentTypeSet componentTypeSet)
            => AddComponent(entityQuery, componentTypeSet, EntityQueryCaptureMode.AtRecord);


        /// <summary>Obsolete. Managed shared components are no longer supported.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <param name="component"> The component value to add. </param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("Managed ISharedComponentData support is deprecated and will be removed. Convert <T> to an unmanaged ISharedComponentData and use the equivalent without the 'Managed' suffix. First deprecated in 6.6.", true)]
        public void AddSharedComponentManaged<T>(EntityQuery entityQuery, T component) where T : struct, ISharedComponentData
            => throw new System.NotSupportedException("Managed components are no longer supported.");

        /// <summary> Records a command to add a unmanaged shared component to all entities matching a query.</summary>
        /// <remarks>
        /// Entities which already have the component type will have the component set to the value.
        /// </remarks>
        /// <param name="entityQuery"> The query specifying which entities to add the component value to. </param>
        /// <param name="component"> The component value to add. </param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddSharedComponent<T>(EntityQuery entityQuery, T component, EntityQueryCaptureMode queryCaptureMode)
            where T : unmanaged, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();

            var isDefaultObject = IsDefaultObjectUnmanaged(ref component, out var hashCode);

            var componentAddr = UnsafeUtility.AddressOf(ref component);
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            if (queryCaptureMode == EntityQueryCaptureMode.AtRecord)
#pragma warning restore 0618
            {
                m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                    &m_Data->m_MainThreadChain,
                    MainThreadSortKey,
                    ECBCommand.AddUnmanagedSharedComponentValueForMultipleEntities,
                    entityQuery,
                    hashCode,
                    isDefaultObject ? null : componentAddr);
            }
            else
            {
                m_Data->AppendEntityQueryComponentCommandWithUnmanagedSharedValue<T>(
                    &m_Data->m_MainThreadChain,
                    MainThreadSortKey,
                    ECBCommand.AddUnmanagedSharedComponentValueForEntityQuery,
                    entityQuery,
                    hashCode,
                    isDefaultObject ? null : componentAddr);
            }
        }
        /// <summary>Obsolete. Use <see cref="AddSharedComponent{T}(EntityQuery,T,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <param name="component"> The component value to add. </param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void AddSharedComponent<T>(EntityQuery entityQuery, T component) where T : unmanaged, ISharedComponentData
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
                => AddSharedComponent(entityQuery, component, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618

        /// <summary>Obsolete. Managed shared components are no longer supported.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <param name="component"> The component value to set. </param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("Managed ISharedComponentData support is deprecated and will be removed. Convert <T> to an unmanaged ISharedComponentData and use the equivalent without the 'Managed' suffix. First deprecated in 6.6.", true)]
        public void SetSharedComponentManaged<T>(EntityQuery entityQuery, T component) where T : struct, ISharedComponentData
            => throw new System.NotSupportedException("Managed components are no longer supported.");

        /// <summary> Records a command to set an unmanaged shared component value on all entities matching a query.</summary>
        /// <remarks>
        /// Fails if any of the entities do not have the type of shared component. [todo: should it be required that the component type is included in the query?]
        /// </remarks>
        /// <param name="entityQuery"> The query specifying which entities to add the component value to. </param>
        /// <param name="component"> The component value to add. </param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void SetSharedComponent<T>(EntityQuery entityQuery, T component, EntityQueryCaptureMode queryCaptureMode)
            where T : unmanaged, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();

            var isDefaultObject = IsDefaultObjectUnmanaged(ref component, out var hashCode);

            var componentAddr = UnsafeUtility.AddressOf(ref component);
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            if (queryCaptureMode == EntityQueryCaptureMode.AtRecord)
#pragma warning restore 0618
                m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                    &m_Data->m_MainThreadChain,
                    MainThreadSortKey,
                    ECBCommand.SetUnmanagedSharedComponentValueForMultipleEntities,
                    entityQuery,
                    hashCode,
                    isDefaultObject ? null : componentAddr);
            else
                m_Data->AppendEntityQueryComponentCommandWithUnmanagedSharedValue<T>(
                    &m_Data->m_MainThreadChain,
                    MainThreadSortKey,
                    ECBCommand.SetUnmanagedSharedComponentValueForEntityQuery,
                    entityQuery,
                    hashCode,
                    isDefaultObject ? null : componentAddr);
        }
        /// <summary>Obsolete. Use <see cref="SetSharedComponent{T}(EntityQuery,T,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <param name="component"> The component value to set. </param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void SetSharedComponent<T>(EntityQuery entityQuery, T component) where T : unmanaged, ISharedComponentData
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => SetSharedComponent(entityQuery, component, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618
        /// <summary>Obsolete. Use <see cref="SetSharedComponent{T}(Unity.Entities.EntityQuery,T,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery"> The query specifying which entities to add the component value to. </param>
        /// <param name="component"> The component value to add. </param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("Use SetSharedComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> SetSharedComponent<T>(*)")]
        public void SetSharedComponentForEntityQuery<T>(EntityQuery entityQuery, T component) where T : unmanaged, ISharedComponentData
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => SetSharedComponent<T>(entityQuery, component, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618

        /// <summary>Records a command to remove a component from all entities matching a query.</summary>
        /// <remarks>
        /// Does not affect entities already missing the component.
        /// </remarks>
        /// <param name="entityQuery">The query specifying the entities from which the component is removed. </param>
        /// <param name="componentType">The types of component to remove.</param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent(EntityQuery entityQuery, ComponentType componentType, EntityQueryCaptureMode queryCaptureMode)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            if (queryCaptureMode == EntityQueryCaptureMode.AtRecord)
#pragma warning restore 0618
                m_Data->AppendMultipleEntitiesComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.RemoveComponentForMultipleEntities, entityQuery, componentType);
            else
                m_Data->AppendEntityQueryComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.RemoveComponentForEntityQuery, entityQuery, componentType);
        }
        /// <summary>Obsolete. Use <see cref="RemoveComponent(EntityQuery,ComponentType,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <param name="componentType">The type of component to add.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void RemoveComponent(EntityQuery entityQuery, ComponentType componentType)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => RemoveComponent(entityQuery, componentType, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618
        /// <summary>Obsolete. Use <see cref="RemoveComponent(EntityQuery, ComponentType, EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities from which the component is removed. </param>
        /// <param name="componentType">The types of component to remove.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("Use RemoveComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> RemoveComponent(*)")]
        public void RemoveComponentForEntityQuery(EntityQuery entityQuery, ComponentType componentType)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => RemoveComponent(entityQuery, componentType, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618


        /// <summary>Records a command to remove a component from all entities matching a query.</summary>
        /// <remarks>
        /// Does not affect entities already missing the component.
        /// </remarks>
        /// <param name="entityQuery">The query specifying the entities from which the component is removed. </param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <typeparam name="T"> The type of component to remove. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent<T>(EntityQuery entityQuery, EntityQueryCaptureMode queryCaptureMode)
            => RemoveComponent(entityQuery, ComponentType.ReadWrite<T>(), queryCaptureMode);
        /// <summary>Obsolete. Use <see cref="RemoveComponent{T}(EntityQuery,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to which the component is added.</param>
        /// <typeparam name="T"> The type of component to remove. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void RemoveComponent<T>(EntityQuery entityQuery)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => RemoveComponent(entityQuery, ComponentType.ReadWrite<T>(), EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618
        /// <summary>Obsolete. Use <see cref="RemoveComponent{T}(EntityQuery)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities from which the component is removed. </param>
        /// <typeparam name="T"> The type of component to remove. </typeparam>
        [SupportedInEntitiesForEach]
        [Obsolete("Use RemoveComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> RemoveComponent<T>(*)")]
        public void RemoveComponentForEntityQuery<T>(EntityQuery entityQuery)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => RemoveComponent<T>(entityQuery, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618


        /// <summary>Records a command to remove multiple components from all entities matching a query.</summary>
        /// <remarks>The set of entities matching the query is 'captured' in the method call, and the recorded command stores an array of all these entities.
        ///
        /// Some matching entities may already be missing some or all of the specified components. After this operation, all matching entities will have none of the components.
        ///
        /// At playback, this command throws an error if one of these entities is destroyed before playback. (With safety checks enabled, an exception is thrown. Without safety checks,
        /// playback will perform invalid and unsafe memory access.)
        /// </remarks>
        /// <param name="entityQuery">The query specifying the entities from which the components are removed. </param>
        /// <param name="componentTypeSet">The types of components to remove.</param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void RemoveComponent(EntityQuery entityQuery, in ComponentTypeSet componentTypeSet, EntityQueryCaptureMode queryCaptureMode)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            if (queryCaptureMode == EntityQueryCaptureMode.AtRecord)
#pragma warning restore 0618
                m_Data->AppendMultipleEntitiesMultipleComponentsCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.RemoveMultipleComponentsForMultipleEntities, entityQuery, componentTypeSet);
            else
                m_Data->AppendEntityQueryComponentTypeSetCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.RemoveMultipleComponentsForEntityQuery, entityQuery, componentTypeSet);
        }
        /// <summary>Obsolete. Use <see cref="RemoveComponent(EntityQuery,in ComponentTypeSet,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities from which the components are removed. </param>
        /// <param name="componentTypeSet">The types of components to remove.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void RemoveComponent(EntityQuery entityQuery, in ComponentTypeSet componentTypeSet)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => RemoveComponent(entityQuery, componentTypeSet, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618
        /// <summary>Obsolete. Use <see cref="RemoveComponent(EntityQuery,in ComponentTypeSet,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities from which the components are removed. </param>
        /// <param name="componentTypeSet">The types of components to remove.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("Use RemoveComponent (RemovedAfter Entities 1.0) (UnityUpgradable) -> RemoveComponent(*)")]
        public void RemoveComponentForEntityQuery(EntityQuery entityQuery, in ComponentTypeSet componentTypeSet)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => RemoveComponent(entityQuery, componentTypeSet, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618

        /// <summary>Records a command to destroy all entities matching a query.</summary>
        /// <param name="entityQuery">The query specifying the entities to destroy.</param>
        /// <param name="queryCaptureMode">Controls when the entities matching <paramref name="entityQuery"/> are computed and captured.</param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void DestroyEntity(EntityQuery entityQuery, EntityQueryCaptureMode queryCaptureMode)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            if (queryCaptureMode == EntityQueryCaptureMode.AtRecord)
#pragma warning restore 0618
                m_Data->AppendMultipleEntitiesCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.DestroyMultipleEntities, entityQuery);
            else
                m_Data->AppendEntityQueryCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                    ECBCommand.DestroyForEntityQuery, entityQuery);
        }
        /// <summary>Obsolete. Use <see cref="DestroyEntity(EntityQuery,EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to destroy.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("This method now takes an extra parameter to control when the query is evaluated. To preserve the current semantics, use EntityQueryCaptureMode.AtRecord (RemovedAfter Entities 2.0)")]
        public void DestroyEntity(EntityQuery entityQuery)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => DestroyEntity(entityQuery, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618
        /// <summary>Obsolete. Use <see cref="DestroyEntity(Unity.Entities.EntityQuery, EntityQueryCaptureMode)"/> instead.</summary>
        /// <param name="entityQuery">The query specifying the entities to destroy.</param>
        [SupportedInEntitiesForEach]
        [Obsolete("Use DestroyEntity (RemovedAfter Entities 1.0) (UnityUpgradable) -> DestroyEntity(*)")]
        public void DestroyEntitiesForEntityQuery(EntityQuery entityQuery)
#pragma warning disable 0618 // EntityQueryCaptureMode.AtRecord is obsolete.
            => DestroyEntity(entityQuery, EntityQueryCaptureMode.AtRecord);
#pragma warning restore 0618

        static bool IsDefaultObject<T>(ref T component, out int hashCode) where T : struct, ISharedComponentData
        {
            var defaultValue = default(T);

            hashCode = TypeManager.GetHashCode(ref component);
            return TypeManager.Equals(ref defaultValue, ref component);
        }

        static bool IsDefaultObjectUnmanaged<T>(ref T component, out int hashCode) where T : unmanaged, ISharedComponentData
        {
            var defaultValue = default(T);

            hashCode = TypeManager.SharedComponentGetHashCode(UnsafeUtility.AddressOf(ref component), TypeManager.GetTypeIndex<T>());
            return TypeManager.SharedComponentEquals(UnsafeUtility.AddressOf(ref defaultValue),
                UnsafeUtility.AddressOf(ref component),
                TypeManager.GetTypeIndex<T>());
        }


        /// <summary> Records a command to add an unmanaged shared component value on an entity.</summary>
        /// <remarks>At playback, this command throws an error if this entity is destroyed before playback,
        /// if adding this shared component exceeds the maximum number of shared components,
        /// or adding a component type makes the archetype too large.</remarks>
        /// <param name="e"> The entity to add the shared component value to. </param>
        /// <param name="sharedComponent"> The shared component value to add. </param>
        /// <typeparam name="T"> The type of shared component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddSharedComponent<T>(Entity e, T sharedComponent) where T : unmanaged, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();

            var componentAddr = UnsafeUtility.AddressOf(ref sharedComponent);

            var isDefaultObject = IsDefaultObjectUnmanaged(ref sharedComponent, out var hashCode);
            m_Data->AddEntityUnmanagedSharedComponentCommand<T>(&m_Data->m_MainThreadChain,
                MainThreadSortKey,
                ECBCommand.AddUnmanagedSharedComponentData,
                e,
                hashCode,
                isDefaultObject ? null : componentAddr);
        }

        /// <summary> Records a command to add an unmanaged shared component value on a NativeArray of entities.</summary>
        /// <remarks>At playback, this command throws an error if any entity is destroyed before playback,
        /// if adding this shared component exceeds the maximum number of shared components,
        /// or adding a component type makes the archetype too large.</remarks>
        /// <param name="entities"> The NativeArray of entities to add the shared component value to. </param>
        /// <param name="sharedComponent"> The shared component value to add. </param>
        /// <typeparam name="T"> The type of shared component to add. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void AddSharedComponent<T>(NativeArray<Entity> entities, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();

            var entitiesCopy = m_Data->CloneEntitiesArray(entities);
            var isDefaultObject = IsDefaultObjectUnmanaged(ref sharedComponent, out var hashCode);

            m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                &m_Data->m_MainThreadChain,
                MainThreadSortKey,
                ECBCommand.AddUnmanagedSharedComponentValueForMultipleEntities,
                entitiesCopy,
                entities.Length,
                hashCode,
                isDefaultObject ? null : UnsafeUtility.AddressOf(ref sharedComponent));
        }

        /// <summary> Records a command to set an unmanaged shared component value on an entity.</summary>
        /// <remarks> At playback, this command throws an error if this entity is destroyed before playback,
        /// if the entity has the <see cref="Prefab"/> tag, or if the entity doesn't have the shared component type.</remarks>
        /// <param name="e"> The entity to set the shared component value of. </param>
        /// <param name="sharedComponent"> The shared component value to set. </param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void SetSharedComponent<T>(Entity e, T sharedComponent) where T : unmanaged, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();

            var isDefaultObject = IsDefaultObjectUnmanaged(ref sharedComponent, out var hashCode);

            var componentAddr = UnsafeUtility.AddressOf(ref sharedComponent);
            m_Data->AddEntityUnmanagedSharedComponentCommand<T>(
                &m_Data->m_MainThreadChain,
                MainThreadSortKey,
                ECBCommand.SetUnmanagedSharedComponentData,
                e,
                hashCode,
                isDefaultObject ? null : componentAddr);
        }

        /// <summary> Records a command to set an unmanaged shared component value on a NativeArray of entities.</summary>
        /// <remarks> At playback, this command throws an error if any entity is destroyed before playback,
        /// if any entity has the <see cref="Prefab"/> tag, or if any entity doesn't have the shared component type.</remarks>
        /// <param name="entities"> The NativeArray of entities to set the shared component value of. </param>
        /// <param name="sharedComponent"> The shared component value to set. </param>
        /// <typeparam name="T"> The type of shared component to set. </typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        [SupportedInEntitiesForEach]
        public void SetSharedComponent<T>(NativeArray<Entity> entities, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();

            var isDefaultObject = IsDefaultObjectUnmanaged(ref sharedComponent, out var hashCode);

            var entitiesCopy = m_Data->CloneEntitiesArray(entities);

            m_Data->AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                &m_Data->m_MainThreadChain,
                MainThreadSortKey,
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
        /// <param name="e">The entity whose LinkedEntityGroup will be referenced.</param>
        /// <param name="mask">The EntityQueryMask that is used to determine which linked entities to add the component to.
        /// Note that EntityQueryMask ignores all query filtering (including chunk filtering and enableable components),
        /// and may thus match more entities than expected.</param>
        /// <param name="component"> The component value to set. </param>
        /// <typeparam name="T"> The type of component to add.</typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        public void AddComponentForLinkedEntityGroup<T>(Entity e, EntityQueryMask mask, T component) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddLinkedEntityGroupComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.AddComponentLinkedEntityGroup, mask, e, component);
        }

        /// <summary>Records a command that adds a component to an entity's <see cref="LinkedEntityGroup"/> based on an <see cref="EntityQueryMask"/>.
        /// Entities in the <see cref="LinkedEntityGroup"/> that don't match the mask will be skipped safely.</summary>
        /// <remarks>At playback, this command throws an error if the entity is destroyed before playback,
        /// or if any of the matching linked entities cannot add the component.</remarks>
        /// <param name="e">The entity whose LinkedEntityGroup will be referenced.</param>
        /// <param name="mask">The EntityQueryMask that is used to determine which linked entities to add the component to.
        /// Note that EntityQueryMask ignores all query filtering (including chunk filtering and enableable components),
        /// and may thus match more entities than expected.</param>
        /// <param name="componentType"> The component type to add. </param>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        public void AddComponentForLinkedEntityGroup(Entity e, EntityQueryMask mask, ComponentType componentType)
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddLinkedEntityGroupTypeCommand(&m_Data->m_MainThreadChain, MainThreadSortKey, ECBCommand.AddComponentLinkedEntityGroup, mask, e, componentType);
        }

        /// <summary>Records a command that sets a component for an entity's <see cref="LinkedEntityGroup"/> based on an <see cref="EntityQueryMask"/>.
        /// Entities in the <see cref="LinkedEntityGroup"/> that don't match the mask will be skipped safely.</summary>
        /// <remarks>At playback, this command throws an error if the entity is destroyed before playback,
        /// if the entity has the <see cref="Prefab"/> tag, or if any of the matching linked entities do not already have the component.</remarks>
        /// <param name="e">The entity whose LinkedEntityGroup will be modified by this command.</param>
        /// <param name="mask">The EntityQueryMask that is used to determine which linked entities to set the component for.
        /// Note that EntityQueryMask ignores all query filtering (including chunk filtering and enableable components),
        /// and may thus match more entities than expected.</param>
        /// <param name="component"> The component value to set. </param>
        /// <typeparam name="T"> The type of component to add.</typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        public void SetComponentForLinkedEntityGroup<T>(Entity e, EntityQueryMask mask, T component) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddLinkedEntityGroupComponentCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.SetComponentLinkedEntityGroup, mask, e, component);
        }

        /// <summary>Records a command that replaces a component value for an entity's <see cref="LinkedEntityGroup"/>.
        /// Entities in the <see cref="LinkedEntityGroup"/> that don't have the component will be skipped safely.</summary>
        /// <remarks>At playback, this command throws an error if the entity is destroyed before playback.</remarks>
        /// <param name="e">The entity whose LinkedEntityGroup will be referenced.</param>
        /// <param name="component"> The component value to set. </param>
        /// <typeparam name="T"> The type of component to add.</typeparam>
        /// <exception cref="NullReferenceException">Throws if an Allocator was not passed in when the EntityCommandBuffer was created.</exception>
        /// <exception cref="InvalidOperationException">Throws if this EntityCommandBuffer has already been played back.</exception>
        public void ReplaceComponentForLinkedEntityGroup<T>(Entity e, T component) where T : unmanaged, IComponentData
        {
            EnforceSingleThreadOwnership();
            AssertDidNotPlayback();
            m_Data->AddEntityComponentTypeWithValueCommand(&m_Data->m_MainThreadChain, MainThreadSortKey,
                ECBCommand.ReplaceComponentLinkedEntityGroup, e, component);
        }
    }
}
