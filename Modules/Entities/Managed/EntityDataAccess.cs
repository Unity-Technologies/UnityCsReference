// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    readonly struct ManagedEntityDataAccess
    {
        [NoAutoStaticsCleanup] // World.Dispose frees its slot.
        volatile static ManagedEntityDataAccess[] s_Instances = { default, default, default, default };

        // AllocHandle and FreeHandle must be called from the main thread
        public static int AllocHandle(ManagedEntityDataAccess instance)
        {
            var count = s_Instances.Length;
            for (int i = 0; i < count; ++i)
            {
                if (s_Instances[i].World == null)
                {
                    s_Instances[i] = instance;
                    return i;
                }
            }

            var newInstances = new ManagedEntityDataAccess[count * 2];
            for (int i = 0; i < count; ++i)
                newInstances[i] = s_Instances[i];
            newInstances[count] = instance;

            s_Instances = newInstances;
            return count;
        }

        public static void FreeHandle(int i)
        {
            s_Instances[i] = default;
        }

        // This is thread safe even if s_Instances is being resized concurrently since both the old and new versions
        // will have the same value at s_Instances[handle] and the reference is updated atomically
        public static ref ManagedEntityDataAccess GetInstance(int handle)
        {
            return ref s_Instances[handle];
        }

        public static ManagedEntityDataAccess Debugger_GetInstance(int handle)
        {
            if (handle >= 0 && handle < s_Instances.Length)
                return s_Instances[handle];
            else
                return default;
        }

        public ManagedEntityDataAccess(World world, CompanionComponentStore store)
        {
            World = world;
            CompanionComponentStore = store;
        }

        readonly public World                 World;
        readonly public CompanionComponentStore CompanionComponentStore;
    }

    [StructLayout(LayoutKind.Sequential)]
    [GenerateTestsForBurstCompatibility]
    [BurstCompile]
    unsafe struct EntityDataAccess : IDisposable
    {
        internal delegate void PlaybackManagedDelegate(IntPtr self);

        // approximate count of entities past which it is faster
        // to form batches when iterating through them all
        internal const int FASTER_TO_BATCH_THRESHOLD = 10;
        internal const int SHARED_COMPONENT_REFERENCES_DEFAULT = 8;

        private static readonly SharedStatic<IntPtr> s_ManagedPlaybackTrampoline =
            SharedStatic<IntPtr>.GetOrCreate<PlaybackManagedDelegate>();

        [NoAutoStaticsCleanup] // Keeps alive the module delegate s_ManagedPlaybackTrampoline points at.
        private static object s_DelegateGCPrevention;

        [ExcludeFromBurstCompatTesting("Managed data access")]
        internal ref ManagedEntityDataAccess ManagedEntityDataAccess => ref ManagedEntityDataAccess.GetInstance(m_ManagedAccessHandle);

        [ExcludeFromBurstCompatTesting("Managed component store")]
        internal CompanionComponentStore CompanionComponentStore => ManagedEntityDataAccess.CompanionComponentStore;

        // These pointer attributes freak debuggers out because they take
        // copies of the root struct when inspecting, and it generally
        // misbehaves from there.

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal EntityComponentStore* EntityComponentStore
        {
            // This is always safe as the EntityDataAccess is always unsafe heap allocated.
            get
            {
                fixed (EntityComponentStore* ptr = &m_EntityComponentStore)
                {
                    return ptr;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal EntityQueryManager* EntityQueryManager
        {
            // This is always safe as the EntityDataAccess is always unsafe heap allocated.
            get
            {
                fixed (EntityQueryManager* ptr = &m_EntityQueryManager)
                {
                    return ptr;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal ComponentDependencyManager* DependencyManager
        {
            // This is always safe as the EntityDataAccess is always unsafe heap allocated.
            get
            {
                fixed (ComponentDependencyManager* ptr = &m_DependencyManager)
                {
                    return ptr;
                }
            }
        }

        [NativeDisableUnsafePtrRestriction] private EntityComponentStore m_EntityComponentStore;
        [NativeDisableUnsafePtrRestriction] private EntityQueryManager m_EntityQueryManager;
        [NativeDisableUnsafePtrRestriction] private ComponentDependencyManager m_DependencyManager;
        [NativeDisableUnsafePtrRestriction] private UnsafeList<int> m_SharedComponentReferenceIndexList;
        [NativeDisableUnsafePtrRestriction] public EntityQuery m_UniversalQuery; // matches all components
        [NativeDisableUnsafePtrRestriction] public EntityQuery m_UniversalQueryWithChunks;
        [NativeDisableUnsafePtrRestriction] public EntityQuery m_UniversalQueryWithSystems;
        [NativeDisableUnsafePtrRestriction] public EntityQuery m_UniversalQueryWithChunksAndSystems;
        [NativeDisableUnsafePtrRestriction] public EntityQuery m_EntityGuidQuery;
        [NativeDisableUnsafePtrRestriction] public WorldUnmanaged m_WorldUnmanaged;

        public int m_CachedEntityGUIDToEntityIndexVersion;
        UntypedUnsafeParallelHashMap m_CachedEntityGUIDToEntityIndex;

        [GenerateTestsForBurstCompatibility(CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public ref UnsafeParallelMultiHashMap<EntityId, Entity> CachedEntityGUIDToEntityIndex
        {
            get
            {
                fixed (void* ptr = &m_CachedEntityGUIDToEntityIndex)
                {
                    return ref UnsafeUtility.AsRef<UnsafeParallelMultiHashMap<EntityId, Entity>>(ptr);
                }
            }
        }

        internal const int kBuiltinEntityQueryCount = 6;

        private int m_ManagedAccessHandle;

        EntityArchetype m_EntityAndSimulateOnlyArchetype;
        private UntypedUnsafeParallelHashMap m_AliveEntityQueries;

        internal ref UnsafeParallelHashMap<ulong, byte> AliveEntityQueries
        {
            get
            {
                fixed (void* ptr = &m_AliveEntityQueries)
                {
                    return ref UnsafeUtility.AsRef<UnsafeParallelHashMap<ulong, byte>>(ptr);
                }
            }
        }

        internal bool IsInExclusiveTransaction => m_DependencyManager.IsInTransaction;

        internal ref StructuralChangesProfiler.Recorder StructuralChangesRecorder => ref m_EntityComponentStore.StructuralChangesRecorder;

        [ExcludeFromBurstCompatTesting("Takes managed World")]
        public static void Initialize(EntityDataAccess* self, World world)
        {

            self->m_SharedComponentReferenceIndexList = new UnsafeList<int>(SHARED_COMPONENT_REFERENCES_DEFAULT, Allocator.Persistent);

            self->m_EntityAndSimulateOnlyArchetype = default;
            self->m_ManagedAccessHandle = -1;

            self->AliveEntityQueries = new UnsafeParallelHashMap<ulong, byte>(32, Allocator.Persistent);
            self->CachedEntityGUIDToEntityIndex = new UnsafeParallelMultiHashMap<EntityId, Entity>(32, Allocator.Persistent);
            self->m_DependencyManager.OnCreate(world.Unmanaged);
            Entities.EntityComponentStore.Create(&self->m_EntityComponentStore, world.SequenceNumber);
            Unity.Entities.EntityQueryManager.Create(&self->m_EntityQueryManager, &self->m_DependencyManager);

            self->m_WorldUnmanaged = world.Unmanaged;

            var managedGuts = new ManagedEntityDataAccess(world, new CompanionComponentStore(self->EntityComponentStore));
            self->m_ManagedAccessHandle = ManagedEntityDataAccess.AllocHandle(managedGuts);
            self->m_EntityComponentStore.m_DebugOnlyManagedAccess = self->m_ManagedAccessHandle;

            var includeGameObjects = EntityQueryOptions.Default;
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludeSystems | includeGameObjects);
            self->m_UniversalQueryWithSystems = self->m_EntityQueryManager.CreateEntityQuery(self, builder);

            builder.Reset();
            builder.WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab | includeGameObjects);
            self->m_UniversalQuery = self->m_EntityQueryManager.CreateEntityQuery(self, builder);

            builder.Reset();
            builder.WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab |
                                EntityQueryOptions.IncludeMetaChunks | includeGameObjects);
            self->m_UniversalQueryWithChunks = self->m_EntityQueryManager.CreateEntityQuery(self, builder);

            builder.Reset();
            builder.WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab |
                                EntityQueryOptions.IncludeMetaChunks | EntityQueryOptions.IncludeSystems | includeGameObjects);
            self->m_UniversalQueryWithChunksAndSystems = self->m_EntityQueryManager.CreateEntityQuery(self, builder);

            builder.Reset();
            builder.WithAllRW<EntityGuid>()
                .WithOptions(EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IncludePrefab);
            self->m_EntityGuidQuery = self->m_EntityQueryManager.CreateEntityQuery(self, builder);

            builder.Dispose();

            self->m_UniversalQuery._GetImpl()->_DisallowDisposing = 1;
            self->m_UniversalQueryWithChunksAndSystems._GetImpl()->_DisallowDisposing = 1;
            self->m_UniversalQueryWithSystems._GetImpl()->_DisallowDisposing = 1;
            self->m_UniversalQueryWithChunks._GetImpl()->_DisallowDisposing = 1;
            self->m_EntityGuidQuery._GetImpl()->_DisallowDisposing = 1;


            if (s_DelegateGCPrevention == null)
            {
                var trampoline = new PlaybackManagedDelegate(PlaybackManagedDelegateInMonoWithWrappedExceptions);
                s_DelegateGCPrevention = trampoline; // Need to hold on to this
                s_ManagedPlaybackTrampoline.Data = Marshal.GetFunctionPointerForDelegate(trampoline);
            }
        }

        [ExcludeFromBurstCompatTesting("calls managed playback")]
        [MonoPInvokeCallback(typeof(PlaybackManagedDelegate))]
        private static void PlaybackManagedDelegateInMonoWithWrappedExceptions(IntPtr target)
        {
            try
            {
                ((EntityDataAccess*) target.ToPointer())->PlaybackCompanionChanges();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [ExcludeFromBurstCompatTesting("Disposes managed lists")]
        public void Dispose()
        {
            m_SharedComponentReferenceIndexList.Dispose();

            m_UniversalQueryWithSystems._GetImpl()->_DisallowDisposing = 0;
            m_UniversalQueryWithChunks._GetImpl()->_DisallowDisposing = 0;
            m_UniversalQuery._GetImpl()->_DisallowDisposing = 0;
            m_UniversalQueryWithChunksAndSystems._GetImpl()->_DisallowDisposing = 0;
            m_EntityGuidQuery._GetImpl()->_DisallowDisposing = 0;
            m_UniversalQueryWithSystems.Dispose();
            m_UniversalQueryWithChunks.Dispose();
            m_UniversalQuery.Dispose();
            m_UniversalQueryWithChunksAndSystems.Dispose();
            m_UniversalQueryWithSystems = default;
            m_UniversalQueryWithChunks = default;
            m_UniversalQuery = default;
            m_UniversalQueryWithChunksAndSystems = default;
            m_EntityGuidQuery.Dispose();
            m_EntityGuidQuery = default;

            // Disposing EntityQueryImpl accesses EntityComponentStore, so they must be disposed first
            var keys = AliveEntityQueries.GetKeyArray(Allocator.Temp);
            for (var i = 0; i < keys.Length; i++)
            {
                var queryPtr = (EntityQueryImpl*)keys[i];
                queryPtr->Dispose();
                EntityQueryImpl.Free(queryPtr);
            }
            AliveEntityQueries.Dispose();
            AliveEntityQueries = default;

            m_DependencyManager.Dispose();
            // CompanionComponentStore.Dispose walks chunks via m_EntityComponentStore to destroy companion
            // GameObjects, so it must run while ECS chunk data is still valid.
            ManagedEntityDataAccess.CompanionComponentStore.Dispose();
            Entities.EntityComponentStore.Destroy(EntityComponentStore);
            Entities.EntityQueryManager.Destroy(EntityQueryManager);

            ManagedEntityDataAccess.FreeHandle(m_ManagedAccessHandle);
            m_ManagedAccessHandle = -1;

            CachedEntityGUIDToEntityIndex.Dispose();
            CachedEntityGUIDToEntityIndex = default;
        }

        public bool Exists(Entity entity)
        {
            return EntityComponentStore->Exists(entity);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void CheckIsStructuralChange()
        {
            if (DependencyManager->ForEachStructuralChange.Depth != 0)
                throw new InvalidOperationException("Structural changes are not allowed while iterating over entities. Please use EntityCommandBuffer instead.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void CheckIsAdditiveStructuralChange(UnsafePtrList<Archetype> archetypes)
        {
            for(var i = 0; i < archetypes.Length; ++i)
                CheckIsAdditiveStructuralChange(archetypes[i]);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void CheckIsAdditiveStructuralChange(Archetype* archetype)
        {
            if (!DependencyManager->ForEachStructuralChange.IsAdditiveStructuralChangePossible(archetype))
                //@TODO: Better error message
                throw new InvalidOperationException("Structural changes are not allowed while iterating over entities. Please use EntityCommandBuffer instead.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void AssertMainThread()
        {
            if (IsInExclusiveTransaction)
                throw new InvalidOperationException("Must be called from the main thread");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void AssertQueryIsValid(EntityQuery query)
        {
            if (!IsQueryValid(query))
                throw new ArgumentException("query is invalid or disposed");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void AssertQueryHasNoEnableableComponents(EntityQuery query)
        {
            if (query._GetImpl()->_QueryData->HasEnableableComponents != 0)
                throw new ArgumentException("EntityQuery objects with types that implement IEnableableComponent are not currently supported by this operation.");
        }

        public bool IsQueryValid(EntityQuery query)
        {
            if (!AliveEntityQueries.ContainsKey((ulong) (IntPtr) query.__impl))
                return false;

            // Also check that the sequence number matches in case the same pointer was given back by malloc.
            return query.__seqno == query.__impl->_SeqNo;
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckMoreThan1024Components(int count)
        {
            if (count + 1 > 1024)
                throw new ArgumentException($"Archetypes can't hold more than 1024 components");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertCountsMatch(int count, int outputCount)
        {
            if (count != outputCount)
                throw new System.ArgumentException(
                    $"srcEntities.Length ({count}) and outputEntities.Length (({outputCount})) must be the same.");
        }

        public void BeforeStructuralChange()
        {
            // This is not an end user error. If there are any managed changes at this point, it indicates there is some
            // (previous) EntityManager change that is not properly playing back the managed changes that were buffered
            // afterward. That needs to be found and fixed.
            CheckIsStructuralChange();
            EntityComponentStore->AssertNoQueuedCompanionCommands();

            if (!m_DependencyManager.IsInTransaction)
                m_DependencyManager.CompleteAllJobsAndInvalidateArrays();
        }

        public EntityComponentStore.ArchetypeChanges BeginStructuralChanges()
        {
            if (!IsInExclusiveTransaction)
                BeforeStructuralChange();
            m_SharedComponentReferenceIndexList.Clear();
            return EntityComponentStore->BeginArchetypeChangeTracking();
        }

        public void PrepareForAdditiveStructuralChanges()
        {
            CompleteForAdditiveStructuralChanges();
        }

        public void PrepareForAdditiveStructuralChanges(Archetype* archetype)
        {
            CheckIsAdditiveStructuralChange(archetype);
            CompleteForAdditiveStructuralChanges();
        }

        public void PrepareForInstantiateAdditiveStructuralChanges(NativeArray<Entity> entities)
        {
            foreach (var entity in entities)
                CheckIsAdditiveStructuralChange(EntityComponentStore->GetArchetype(entity)->InstantiateArchetype);

            CompleteForAdditiveStructuralChanges();
        }

        public void PrepareForCopyAdditiveStructuralChanges(NativeArray<Entity> entities)
        {
            foreach (var entity in entities)
                CheckIsAdditiveStructuralChange(EntityComponentStore->GetArchetype(entity)->CopyArchetype);

            CompleteForAdditiveStructuralChanges();
        }

        private void CompleteForAdditiveStructuralChanges()
        {
            if (IsInExclusiveTransaction)
                return;

            // This is not an end user error. If there are any managed changes at this point, it indicates there is some
            // (previous) EntityManager change that is not properly playing back the managed changes that were buffered
            // afterward. That needs to be found and fixed.

            EntityComponentStore->AssertNoQueuedCompanionCommands();

            if (!m_DependencyManager.IsInTransaction)
                DependencyManager->CompleteAllJobsAndCheckDeallocateAndThrow();
        }

        public EntityComponentStore.ArchetypeChanges BeginAdditiveStructuralChanges()
        {
            m_SharedComponentReferenceIndexList.Clear();
            return EntityComponentStore->BeginArchetypeChangeTracking();
        }

        public void CheckIsAdditiveArchetypeStructuralChangePossible(Archetype* archetype)
        {
            if (!IsInExclusiveTransaction)
                CheckIsAdditiveStructuralChange(archetype);
        }

        public void EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes)
        {
            if (EntityComponentStore->CompanionChangesTracker.CommandBuffer.Length > 0)
                PlaybackCompanionChanges();

            if (!m_SharedComponentReferenceIndexList.IsEmpty)
                ResetSharedComponentReferences();

            if (EntityComponentStore->m_Archetypes.Length - changes.StartIndex != 0)
                EntityComponentStore->EndArchetypeChangeTracking(changes, EntityQueryManager);

            if (EntityComponentStore->m_ChunkListChangesTracker.ArchetypeTrackingHead != null)
                EntityComponentStore->InvalidateChunkListCacheForChangedArchetypes();

        }


        internal void ResetSharedComponentReferences()
        {
            fixed (EntityDataAccess* mgr = &this)
            {
                ECBInterop.RemoveSharedComponentReferences(mgr, m_SharedComponentReferenceIndexList.Ptr,
                    m_SharedComponentReferenceIndexList.Length);
            }

            m_SharedComponentReferenceIndexList.Clear();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entities">The NativeArray of entities to destroy</param>
        public void DestroyEntityDuringStructuralChange(NativeArray<Entity> entities,
            in SystemHandle originSystem = default)
        {
            if (entities.Length == 0)
                return;

            EntityComponentStore->AssertValidEntities((Entity*) entities.GetUnsafePtr(), entities.Length);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.DestroyEntity, in m_WorldUnmanaged);

            StructuralChange.DestroyEntity(EntityComponentStore, (Entity*) entities.GetUnsafePtr(), entities.Length);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="queryImpl"></param>
        public void DestroyEntitiesInQueryDuringStructuralChange(EntityQueryImpl* queryImpl,
            in SystemHandle originSystem = default)
        {
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.DestroyEntity, in m_WorldUnmanaged);
            queryImpl->SyncFilterTypes();
            var linkedEntityGroupTypeHandle = GetBufferTypeHandle<LinkedEntityGroup>(true);
            StructuralChange.DestroyChunksQuery(EntityComponentStore, queryImpl, ref linkedEntityGroupTypeHandle);
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="count"></param>
        internal void DestroyEntityInternalDuringStructuralChange(Entity* entities, int count,
            in SystemHandle originSystem = default)
        {
            if (count == 0)
                return;
            EntityComponentStore->AssertValidEntities(entities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.DestroyEntity, in m_WorldUnmanaged);

            StructuralChange.DestroyEntity(EntityComponentStore, entities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }


        internal EntityArchetype CreateArchetype(ComponentType* types, int count, bool addSimulateComponentIfMissing)
        {
            Assert.IsTrue(types != null || count == 0);
            EntityComponentStore->AssertCanCreateArchetype(types, count);

            ComponentTypeInArchetype* typesInArchetype = stackalloc ComponentTypeInArchetype[count + 2];

            var cachedComponentCount = FillSortedArchetypeArray(typesInArchetype, types, count, addSimulateComponentIfMissing);

            return CreateArchetype_Sorted(typesInArchetype, cachedComponentCount);
        }

        internal EntityArchetype CreateArchetype_Sorted(ComponentTypeInArchetype* typesInArchetype, int cachedComponentCount)
        {
            // Lookup existing archetype (cheap)
            EntityArchetype entityArchetype;
            entityArchetype.Archetype = EntityComponentStore->GetExistingArchetype(typesInArchetype, cachedComponentCount);
            if (entityArchetype.Archetype != null)
                return entityArchetype;

            // Creating an archetype invalidates all iterators / jobs etc
            // because it affects the live iteration linked lists...

            entityArchetype.Archetype = EntityComponentStore->GetOrCreateArchetype(typesInArchetype, cachedComponentCount);

            return entityArchetype;
        }

        internal static int FillSortedArchetypeArray(ComponentTypeInArchetype* dst, ComponentType* requiredComponents, int count, bool addSimulateComponentIfMissing)
        {
            CheckMoreThan1024Components(count);
            dst[0] = new ComponentTypeInArchetype(ComponentType.ReadWrite<Entity>());
            bool hasSimulate = false;
            for (var i = 0; i < count; ++i)
            {
                hasSimulate |= (requiredComponents[i] == ComponentType.ReadWrite<Simulate>());
                SortingUtilities.InsertSorted(dst, i + 1, requiredComponents[i]);
            }
            if (!hasSimulate && addSimulateComponentIfMissing)
            {
                SortingUtilities.InsertSorted(dst, count + 1, ComponentType.ReadWrite<Simulate>());
                return count + 2;
            }
            return count + 1;
        }

        /// <summary>
        /// Creates an entity with no components.
        /// </summary>
        /// <remarks>
        /// Creates the entity in the first available chunk with the archetype having no components.
        ///
        /// **Important:** This function creates a sync point, which means that the EntityManager waits for all
        /// currently running Jobs to complete before creating the entity and no additional Jobs can start before
        /// the function is finished. A sync point can cause a drop in performance because the ECS framework may not
        /// be able to make use of the processing power of all available cores.
        /// </remarks>
        /// <returns>The Entity object that you can use to access the entity.</returns>
        public Entity CreateEntity(in SystemHandle originSystem = default)
        {
            Entity entity;

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in m_WorldUnmanaged);

            StructuralChange.CreateEntity(EntityComponentStore, GetEntityAndSimulateArchetype().Archetype, &entity, 1);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();

            return entity;
        }

        public void AllocateAndAssignChunksToExistingEntities(EntityArchetype archetype, Entity* existingEntities, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count must be non-negative");
            Unity.Entities.EntityComponentStore.AssertValidArchetype(EntityComponentStore, archetype);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in m_WorldUnmanaged);

            StructuralChange.AllocateAndAssignChunksToExistingEntities(EntityComponentStore, archetype.Archetype, existingEntities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        public ChunkIndex GetChunkWithEmptySlots( EntityArchetype entityArchetype)
        {
            Unity.Entities.EntityComponentStore.AssertValidArchetype(EntityComponentStore, entityArchetype);

            var archetypeChunkFilter = new EntityComponentStore.ArchetypeChunkFilter();
            archetypeChunkFilter.Archetype = entityArchetype.Archetype;
            return EntityComponentStore->GetChunkWithEmptySlots(ref archetypeChunkFilter);
        }

        public void DeallocateAndUnAssignChunksToExistingEntities(Entity* existingEntities, int count)
        {
            if (count == 0)
                return;
            EntityComponentStore->AssertValidEntities(existingEntities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.DestroyEntity, in m_WorldUnmanaged);
            StructuralChange.DeallocateAndUnAssignChunksToExistingEntities(EntityComponentStore, existingEntities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="archetype"></param>
        /// <exception cref="ArgumentException">Thrown if the archetype is null.</exception>
        /// <returns></returns>
        public Entity CreateEntityDuringStructuralChange(EntityArchetype archetype, in SystemHandle originSystem = default)
        {
            Entity entity;
            CreateEntityDuringStructuralChange(archetype, &entity, 1, in originSystem);
            return entity;
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="archetype"></param>
        /// <param name="outEntities"></param>
        /// <param name="count"></param>
        /// <exception cref="ArgumentException">Thrown if the archetype is null.</exception>
        internal void CreateEntityDuringStructuralChange(EntityArchetype archetype, Entity* outEntities, int count,
            in SystemHandle originSystem = default)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count must be non-negative");
            Unity.Entities.EntityComponentStore.AssertValidArchetype(EntityComponentStore, archetype);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in m_WorldUnmanaged);

            StructuralChange.CreateEntity(EntityComponentStore, archetype.Archetype, outEntities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="archetype"></param>
        /// <param name="entities"></param>
        public void CreateEntityDuringStructuralChange(EntityArchetype archetype, NativeArray<Entity> entities, in SystemHandle originSystem = default)
        {
            CreateEntityDuringStructuralChange(archetype, (Entity*) entities.GetUnsafePtr(), entities.Length, in originSystem);
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="componentType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if an entity does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if the component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public bool AddComponentDuringStructuralChange(Entity entity, ComponentType componentType,
            in SystemHandle originSystem = default)
        {
            if (HasComponent(entity, componentType))
                return false;

            EntityComponentStore->AssertCanAddComponent(entity, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            var result = StructuralChange.AddComponentEntity(EntityComponentStore, &entity, componentType.TypeIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
            return result;
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// Adds a component and assigns its initial value atomically. If the entity already has the component,
        /// the value is assigned without a structural change.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="componentType"></param>
        /// <param name="componentValue">The initial value for the component. Must not be null; for zero-sized
        /// components use <see cref="AddComponentDuringStructuralChange(Entity,ComponentType,in SystemHandle)"/>.</param>
        /// <param name="componentSize">The size in bytes of <paramref name="componentValue"/>.</param>
        /// <returns>True if the component was added, false if the entity already had it.</returns>
        /// <exception cref="InvalidOperationException">Thrown if an entity does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if the component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public bool AddComponentDuringStructuralChange(Entity entity, ComponentType componentType,
            void* componentValue, int componentSize, in SystemHandle originSystem = default)
        {
            if (HasComponent(entity, componentType))
            {
                if (componentType.IsBuffer)
                    SetBufferRaw(entity, componentType.TypeIndex, (BufferHeader*)componentValue, componentSize, in originSystem);
                else
                    SetComponentDataRaw(entity, componentType.TypeIndex, componentValue, componentSize, in originSystem);
                return false;
            }

            EntityComponentStore->AssertCanAddComponent(entity, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            var result = StructuralChange.AddComponentEntityWithValue(EntityComponentStore, &entity, componentType.TypeIndex,
                (byte*)componentValue, componentSize);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
            return result;
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="typeSet"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if an entity does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if the component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public void AddMultipleComponentsDuringStructuralChange(Entity entity, in ComponentTypeSet typeSet,
            in SystemHandle originSystem = default)
        {
            EntityComponentStore->AssertCanAddComponents(entity, typeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            StructuralChange.AddComponentsEntity(EntityComponentStore, &entity, typeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="queryImpl">The query to apply this operation to</param>
        /// <param name="componentType">The component type being added to the entities.</param>
        /// <exception cref="InvalidOperationException">Thrown if an entity in the query does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if the component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public void AddComponentToQueryDuringStructuralChange(EntityQueryImpl* queryImpl, ComponentType componentType,
            in SystemHandle originSystem = default)
        {
            if (queryImpl->IsEmptyIgnoreFilter)
                return;
            EntityComponentStore->AssertCanAddComponent(queryImpl, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            queryImpl->SyncFilterTypes();
            StructuralChange.AddComponentQuery(EntityComponentStore, queryImpl, componentType.TypeIndex);
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// Like <see cref="AddComponentToQueryDuringStructuralChange"/> but defers OnAdded callbacks, recording
        /// destination ranges in <paramref name="deferredOnAddedRanges"/>. The caller must fire them via
        /// <see cref="EntityComponentStore.FireDeferredOnAddedCallbacks"/> after assigning per-entity values.
        /// </summary>
        /// <param name="queryImpl">The query to apply this operation to</param>
        /// <param name="componentType">The component type being added to the entities.</param>
        /// <param name="deferredOnAddedRanges">Receives the destination ranges of the entities the component was added to.</param>
        /// <exception cref="InvalidOperationException">Thrown if an entity in the query does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if the component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public void AddComponentToQueryDuringStructuralChange(EntityQueryImpl* queryImpl, ComponentType componentType,
            UnsafeList<EntityBatchInChunk>* deferredOnAddedRanges, in SystemHandle originSystem = default)
        {
            if (queryImpl->IsEmptyIgnoreFilter)
                return;
            EntityComponentStore->AssertCanAddComponent(queryImpl, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            queryImpl->SyncFilterTypes();
            StructuralChange.AddComponentQueryWithDeferredOnAddedCallbacks(EntityComponentStore, queryImpl,
                componentType.TypeIndex, deferredOnAddedRanges);
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="queryImpl">The query to apply this operation to</param>
        /// <param name="typeSet">The component types being added to the entities.</param>
        /// <exception cref="InvalidOperationException">Thrown if an entity in the query does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if one of the component types being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if one of the components increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if one of the components causes the size of the archetype to exceed the size of a chunk.</exception>
        internal void AddComponentsToQueryDuringStructuralChange(EntityQueryImpl* queryImpl, in ComponentTypeSet typeSet,
            in SystemHandle originSystem = default)
        {
            if (queryImpl->IsEmptyIgnoreFilter || typeSet.Length == 0)
                return;
            EntityComponentStore->AssertCanAddComponents(queryImpl->_QueryData->MatchingArchetypes, typeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            queryImpl->SyncFilterTypes();
            StructuralChange.AddComponentsQuery(EntityComponentStore, queryImpl, typeSet);
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entities">The set of entities.</param>
        /// <param name="componentType">The component type added to the entities.</param>
        /// <exception cref="InvalidOperationException">Thrown if an entity in the native array does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if a component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public void AddComponentDuringStructuralChange(NativeArray<Entity> entities, ComponentType componentType,
            in SystemHandle originSystem = default)
        {
            if (entities.Length == 0)
                return;

            EntityComponentStore->AssertCanAddComponent(entities, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            if (entities.Length > FASTER_TO_BATCH_THRESHOLD &&
                EntityComponentStore->CreateEntityBatchList(entities, componentType.IsSharedComponent ? 1 : 0,
                    Allocator.Temp, out var entityBatchList))
            {
                StructuralChange.AddComponentEntitiesBatch(EntityComponentStore, entityBatchList.GetUnsafeList(), componentType.TypeIndex);
            }
            else
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    StructuralChange.AddComponentEntity(EntityComponentStore, &entity, componentType.TypeIndex);
                }
            }

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// Adds a component and assigns the same initial value to every entity atomically. Entities that
        /// already have the component have the value assigned without a structural change.
        /// </summary>
        /// <param name="entities">The set of entities.</param>
        /// <param name="componentType">The component type added to the entities. May not be a buffer component.</param>
        /// <param name="componentValue">The initial value for the component. Must not be null.</param>
        /// <param name="componentSize">The size in bytes of <paramref name="componentValue"/>.</param>
        /// <exception cref="InvalidOperationException">Thrown if an entity in the native array does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if a component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public void AddComponentDuringStructuralChange(NativeArray<Entity> entities, ComponentType componentType,
            void* componentValue, int componentSize, in SystemHandle originSystem = default)
        {
            if (entities.Length == 0)
                return;

            EntityComponentStore->AssertCanAddComponent(entities, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            if (entities.Length > FASTER_TO_BATCH_THRESHOLD &&
                EntityComponentStore->CreateEntityBatchList(entities, 0, Allocator.Temp, out var entityBatchList))
            {
                StructuralChange.AddComponentEntitiesBatchWithValue(EntityComponentStore, entityBatchList.GetUnsafeList(),
                    componentType.TypeIndex, (byte*)componentValue, componentSize);
            }
            else
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    StructuralChange.AddComponentEntityWithValue(EntityComponentStore, &entity, componentType.TypeIndex,
                        (byte*)componentValue, componentSize);
                }
            }

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entities">The set of entities.</param>
        /// <param name="componentTypeSet">The component types added to the entities.</param>
        /// <exception cref="InvalidOperationException">Thrown if an entity in the native array does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if a component type being added is Entity type.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component increases the shared component count of the entity's archetype to more than the maximum allowed.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the component causes the size of the archetype to exceed the size of a chunk.</exception>
        public void AddMultipleComponentsDuringStructuralChange(NativeArray<Entity> entities,
            in ComponentTypeSet componentTypeSet, in SystemHandle originSystem = default)
        {
            if (entities.Length == 0 || componentTypeSet.Length == 0)
                return;
            EntityComponentStore->AssertCanAddComponents(entities, componentTypeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);

            if (componentTypeSet.ChunkComponentCount == 0 && entities.Length > FASTER_TO_BATCH_THRESHOLD &&
                EntityComponentStore->CreateEntityBatchList(entities, componentTypeSet.m_masks.SharedComponents,
                    Allocator.Temp, out var entityBatchList))
            {
                StructuralChange.AddComponentsEntitiesBatch(EntityComponentStore, entityBatchList.GetUnsafeList(), componentTypeSet);
            }
            else
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    StructuralChange.AddComponentsEntity(EntityComponentStore, &entity, componentTypeSet);
                }
            }

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entityArray"></param>
        /// <param name="componentTypeSet"></param>
        /// <exception cref="InvalidOperationException">Thrown if a componentType is Entity type.</exception>
        public void RemoveMultipleComponentsDuringStructuralChange(NativeArray<Entity> entities,
            in ComponentTypeSet componentTypeSet, in SystemHandle originSystem = default)
        {
            if (entities.Length == 0 || componentTypeSet.Length == 0)
                return;
            EntityComponentStore->AssertCanRemoveComponents(componentTypeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.RemoveComponent, in m_WorldUnmanaged);

            if (entities.Length > FASTER_TO_BATCH_THRESHOLD &&
                EntityComponentStore->CreateEntityBatchList(entities, 0, Allocator.Temp, out var entityBatchList))
            {
                StructuralChange.RemoveComponentsEntitiesBatch(EntityComponentStore, entityBatchList.GetUnsafeList(), componentTypeSet);
            }
            else
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    StructuralChange.RemoveComponentsEntity(EntityComponentStore, &entity, componentTypeSet);
                }
            }

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="componentType"></param>
        /// <returns></returns>
        public bool RemoveComponentDuringStructuralChange(Entity entity, ComponentType componentType,
            in SystemHandle originSystem = default)
        {
            EntityComponentStore->AssertCanRemoveComponent(componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.RemoveComponent, in m_WorldUnmanaged);

            var result = StructuralChange.RemoveComponentEntity(EntityComponentStore, &entity, componentType.TypeIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();

            return result;
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="componentTypeSet"></param>
        public void RemoveComponentDuringStructuralChange(Entity entity, in ComponentTypeSet componentTypeSet,
            in SystemHandle originSystem = default)
        {
            EntityComponentStore->AssertCanRemoveComponents(componentTypeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.RemoveComponent, in m_WorldUnmanaged);

            StructuralChange.RemoveComponentsEntity(EntityComponentStore, &entity, componentTypeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="queryImpl"></param>
        /// <param name="componentType"></param>
        /// <exception cref="InvalidOperationException">Thrown if the componentType is Entity type.</exception>
        public void RemoveComponentFromQueryDuringStructuralChange(EntityQueryImpl* queryImpl, ComponentType componentType,
            in SystemHandle originSystem = default)
        {
            if (queryImpl->IsEmptyIgnoreFilter)
                return;
            EntityComponentStore->AssertCanRemoveComponent(componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.RemoveComponent, in m_WorldUnmanaged);
            queryImpl->SyncFilterTypes();
            StructuralChange.RemoveComponentQuery(EntityComponentStore, queryImpl, componentType.TypeIndex);
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="queryImpl"></param>
        /// <param name="componentTypeSet"></param>
        internal void RemoveMultipleComponentsFromQueryDuringStructuralChange(EntityQueryImpl* queryImpl, in ComponentTypeSet componentTypeSet,
            in SystemHandle originSystem = default)
        {
            if (queryImpl->IsEmptyIgnoreFilter || componentTypeSet.Length == 0)
                return;
            EntityComponentStore->AssertCanRemoveComponents(componentTypeSet);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.RemoveComponent, in m_WorldUnmanaged);
            queryImpl->SyncFilterTypes();
            StructuralChange.RemoveComponentsQuery(EntityComponentStore, queryImpl, componentTypeSet);
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="componentType"></param>
        internal void RemoveComponentDuringStructuralChange(NativeArray<ArchetypeChunk> chunks,
            ComponentType componentType, in SystemHandle originSystem = default)
        {
            if (chunks.Length == 0)
                return;

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.RemoveComponent, in m_WorldUnmanaged);

            StructuralChange.RemoveComponentChunks(EntityComponentStore,
                (ArchetypeChunk*) NativeArrayUnsafeUtility.GetUnsafePtr(chunks), chunks.Length,
                componentType.TypeIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="entityArray"></param>
        /// <param name="componentType"></param>
        /// <exception cref="ArgumentException">Thrown if componentType is Entity type.</exception>
        public void RemoveComponentDuringStructuralChange(NativeArray<Entity> entities, ComponentType componentType,
            in SystemHandle originSystem = default)
        {
            if (entities.Length == 0)
                return;

            EntityComponentStore->AssertCanRemoveComponent(componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.RemoveComponent, in m_WorldUnmanaged);

            if (entities.Length > FASTER_TO_BATCH_THRESHOLD &&
                EntityComponentStore->CreateEntityBatchList(entities, 0, Allocator.Temp, out var entityBatchList))
            {
                StructuralChange.RemoveComponentEntitiesBatch(EntityComponentStore, entityBatchList.GetUnsafeList(), componentType.TypeIndex);
            }
            else
            {
                for (var i = 0; i < entities.Length; ++i)
                {
                    var entity = entities[i];
                    StructuralChange.RemoveComponentEntity(EntityComponentStore, &entity, componentType.TypeIndex);
                }
            }

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        public bool HasComponent(Entity entity, ComponentType type)
        {
            return EntityComponentStore->HasComponent(entity, type, out _);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(BurstCompatibleComponentData)})]
        public T GetComponentData<T>(Entity entity) where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();

            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            EntityComponentStore->AssertNotZeroSizedComponent(typeIndex);

            if (!IsInExclusiveTransaction)
                DependencyManager->CompleteWriteDependency(typeIndex);

            var ptr = EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);

            T value;
            UnsafeUtility.CopyPtrToStructure(ptr, out value);
            return value;
        }

        // This complements the non-RW version above, completing dependencies. It must return byte* because we end up
        // inserting into a RefRW in EntityManager.
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public byte* GetComponentDataRW_AsBytePointer(Entity entity, TypeIndex typeIndex)
        {
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            EntityComponentStore->AssertNotZeroSizedComponent(typeIndex);
            EntityComponentStore->AssertComponentIsUnmanaged(typeIndex);

            if (!IsInExclusiveTransaction)
                DependencyManager->CompleteReadAndWriteDependency(typeIndex);

            return EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex, EntityComponentStore->GlobalSystemVersion);
        }

        // Does not complete dependencies - used for low level component data memory access
        public void* GetComponentDataRawRW(Entity entity, TypeIndex typeIndex)
        {
            return EntityComponentStore->GetComponentDataRawRW(entity, typeIndex);
        }

        internal void* GetComponentDataRawRWEntityHasComponent(Entity entity, TypeIndex typeIndex)
        {
            return EntityComponentStore->GetComponentDataRawRWEntityHasComponent(entity, typeIndex);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(BurstCompatibleComponentData)})]
        public void SetComponentData<T>(Entity entity, T componentData, in SystemHandle originSystem = default)
            where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();

            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            EntityComponentStore->AssertNotZeroSizedComponent(typeIndex);

            if (!IsInExclusiveTransaction)
                DependencyManager->CompleteReadAndWriteDependency(typeIndex);

            var ptr = EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                EntityComponentStore->GlobalSystemVersion);
            UnsafeUtility.CopyStructureToPtr(ref componentData, ptr);
        }

        public void SetComponentDataRaw(Entity entity, TypeIndex typeIndex, void* data, int size,
            in SystemHandle originSystem = default)
        {
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            EntityComponentStore->SetComponentDataRawEntityHasComponent(entity, typeIndex, data, size);
        }

        public bool IsComponentEnabled(Entity entity, TypeIndex typeIndex)
        {
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            Unity.Entities.EntityComponentStore.AssertComponentEnableable(typeIndex);

            return EntityComponentStore->IsComponentEnabled(entity, typeIndex);
        }

        public bool IsComponentEnabled(Entity entity, TypeIndex typeIndex, ref LookupCache typeLookupCache)
        {
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex, ref typeLookupCache);
            Unity.Entities.EntityComponentStore.AssertComponentEnableable(typeIndex);

            return EntityComponentStore->IsComponentEnabled(entity, typeIndex, ref typeLookupCache);
        }

        public void SetComponentEnabled(Entity entity, TypeIndex typeIndex, bool value)
        {
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            Unity.Entities.EntityComponentStore.AssertComponentEnableable(typeIndex);

            EntityComponentStore->SetComponentEnabled(entity, typeIndex, value);
        }

        public void SetComponentEnabled(Entity entity, TypeIndex typeIndex, bool value, ref LookupCache typeLookupCache)
        {
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex, ref typeLookupCache);
            Unity.Entities.EntityComponentStore.AssertComponentEnableable(typeIndex);

            EntityComponentStore->SetComponentEnabled(entity, typeIndex, value, ref typeLookupCache);
        }

        public bool IsEnabled(Entity entity)
        {
            return !HasComponent(entity, ComponentType.ReadWrite<Disabled>());
        }

        public void SetEnabled(Entity entity, bool value)
        {
            if (IsEnabled(entity) == value)
                return;

            var disabledType = ComponentType.ReadWrite<Disabled>();
            if (HasComponent(entity, ComponentType.ReadWrite<LinkedEntityGroup>()))
            {
                var typeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>();
                //@TODO DOTS-5412: We can't use WorldUpdate.Allocator here because DynamicBuffer.ToNativeArray() uses new NativeArray() instead of CollectionHelper.CreateNativeArray()
                var linkedEntities = GetBuffer<LinkedEntityGroup>(entity
                    , DependencyManager->Safety.GetSafetyHandle(typeIndex, false),
                    DependencyManager->Safety.GetBufferSafetyHandle(typeIndex)
                ).Reinterpret<Entity>()
                    .ToNativeArray(Allocator.TempJob);
                {
                    if (value)
                        RemoveComponentDuringStructuralChange(linkedEntities, disabledType);
                    else
                        AddComponentDuringStructuralChange(linkedEntities, disabledType);
                }
                linkedEntities.Dispose();
            }
            else
            {
                if (!value)
                    AddComponentDuringStructuralChange(entity, disabledType);
                else
                    RemoveComponentDuringStructuralChange(entity, disabledType);
            }
        }

        public void AddComponentForLinkedEntityGroup(Entity entity, EntityQueryMask mask, TypeIndex typeIndex, void* data, int componentSize)
        {
            if (!HasComponent(entity, ComponentType.ReadWrite<LinkedEntityGroup>()))
                return;

            var linkedTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>();
            using var linkedEntities = GetBuffer<LinkedEntityGroup>(entity
                    , DependencyManager->Safety.GetSafetyHandle(linkedTypeIndex, false),
                    DependencyManager->Safety.GetBufferSafetyHandle(linkedTypeIndex)
                ).Reinterpret<Entity>()
                .ToNativeArray(m_WorldUnmanaged.UpdateAllocator.ToAllocator);

            // Filter the linked entities based on the mask
            foreach (var e in linkedEntities)
            {
                if (mask.MatchesIgnoreFilter(e))
                {
                    if (componentSize > 0)
                        AddComponentDuringStructuralChange(e, ComponentType.FromTypeIndex(typeIndex), data, componentSize);
                    else
                        AddComponentDuringStructuralChange(e, ComponentType.FromTypeIndex(typeIndex));
                }
            }
        }

        public void SetComponentForLinkedEntityGroup(Entity entity, EntityQueryMask mask, TypeIndex typeIndex, void* data, int componentSize)
        {
            if (!HasComponent(entity, ComponentType.ReadWrite<LinkedEntityGroup>()))
                return;

            EntityComponentStore->AssertNotZeroSizedComponent(typeIndex);

            var linkedTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>();
            using var linkedEntities = GetBuffer<LinkedEntityGroup>(entity
                    , DependencyManager->Safety.GetSafetyHandle(linkedTypeIndex, false),
                    DependencyManager->Safety.GetBufferSafetyHandle(linkedTypeIndex)
                ).Reinterpret<Entity>()
                .ToNativeArray(m_WorldUnmanaged.UpdateAllocator.ToAllocator);

            // Filter the linked entities based on the mask
            foreach (var e in linkedEntities)
            {
                if (mask.MatchesIgnoreFilter(e))
                {
                    EntityComponentStore->AssertEntityHasComponent(e, typeIndex);

                    SetComponentDataRaw(e, typeIndex, data, componentSize);
                }
            }
        }

        public void ReplaceComponentForLinkedEntityGroup(Entity entity, TypeIndex typeIndex, void* data, int componentSize)
        {
            if (!HasComponent(entity, ComponentType.ReadWrite<LinkedEntityGroup>()))
                return;

            EntityComponentStore->AssertNotZeroSizedComponent(typeIndex);

            var linkedTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>();
            using var linkedEntities = GetBuffer<LinkedEntityGroup>(entity
                    , DependencyManager->Safety.GetSafetyHandle(linkedTypeIndex, false),
                    DependencyManager->Safety.GetBufferSafetyHandle(linkedTypeIndex)
                ).Reinterpret<Entity>()
                .ToNativeArray(m_WorldUnmanaged.UpdateAllocator.ToAllocator);

            // Filter the linked entities based on the mask
            foreach (var e in linkedEntities)
            {
                if (EntityComponentStore->HasComponent(e, typeIndex, out _))
                {
                    SetComponentDataRaw(e, typeIndex, data, componentSize);
                }
            }
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="chunks"></param>
        /// <param name="sharedComponentIndex"></param>
        /// <param name="componentType"></param>
        internal void AddSharedComponentDataDuringStructuralChange(
            NativeArray<ArchetypeChunk> chunks,
            int sharedComponentIndex,
            ComponentType componentType,
            in SystemHandle originSystem = default)
        {
            Assert.IsTrue(componentType.IsSharedComponent);
            if (chunks.Length == 0)
                return;

            StructuralChange.AddSharedComponentChunks(EntityComponentStore,
                (ArchetypeChunk*) NativeArrayUnsafeUtility.GetUnsafePtr(chunks), chunks.Length, componentType.TypeIndex,
                sharedComponentIndex);
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="queryImpl"></param>
        /// <param name="sharedComponentIndex"></param>
        /// <param name="componentType"></param>
        /// <param name="componentValue"></param>
        internal void AddSharedComponentDataToQueryDuringStructuralChange_Unmanaged(
            EntityQueryImpl* queryImpl,
            int sharedComponentIndex,
            ComponentType componentType,
            void* componentValue,
            in SystemHandle originSystem = default)
        {
            Assert.IsTrue(componentType.IsSharedComponent);
            if (queryImpl->IsEmptyIgnoreFilter)
                return;
            EntityComponentStore->AssertCanAddComponent(queryImpl, componentType);
            if (StructuralChangesProfiler.Enabled)
            {
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);
            }
            queryImpl->SyncFilterTypes();
            StructuralChange.AddSharedComponentQuery(EntityComponentStore, queryImpl, componentType.TypeIndex, sharedComponentIndex);
            if (StructuralChangesProfiler.Enabled)
            {
                StructuralChangesRecorder.End(); // SetSharedComponent
                StructuralChangesRecorder.End(); // AddComponent
            }
        }

        [GenerateTestsForBurstCompatibility]
        public void AddSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(NativeArray<Entity> entities, TypeIndex typeIndex, int hashCode, void* componentDataAddr)
        {
            //TODO: optimization: set value when component is added, not afterwards
            AddComponentDuringStructuralChange(entities, ComponentType.FromTypeIndex(typeIndex));
            SetSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(entities, typeIndex, hashCode, componentDataAddr);
        }

        [GenerateTestsForBurstCompatibility]
        // if componentData is null: consider we are adding the default value
        // if defaultComponentData is null: consider we are inserting non default value
        public bool AddSharedComponentDataDuringStructuralChange_Unmanaged(Entity entity, ComponentType componentType, void* componentData, void* defaultComponentData)
        {
            var added = AddComponentDuringStructuralChange(entity, componentType);
            SetSharedComponentData_Unmanaged(entity, componentType.TypeIndex, componentData, defaultComponentData);
            return added;
        }

        [GenerateTestsForBurstCompatibility]
        // if componentData is null: consider we are adding the default value
        // if defaultComponentData is null: consider we are inserting non default value
        public void AddSharedComponentDataDuringStructuralChange_Unmanaged(NativeArray<Entity> entities,
            ComponentType componentType, void* componentData, void* defaultComponentData, in SystemHandle originSystem = default)
        {
            if (StructuralChangesProfiler.Enabled)
            {
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.AddComponent, in m_WorldUnmanaged);
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);
            }

            var newSharedComponentDataIndex = InsertSharedComponent_Unmanaged(componentType.TypeIndex, 0, componentData, defaultComponentData);
            StructuralChange.AddSharedComponentDataIndexWithBurst(EntityComponentStore, (Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length, componentType,
                newSharedComponentDataIndex);

            if (StructuralChangesProfiler.Enabled)
            {
                StructuralChangesRecorder.End(); // SetSharedComponent
                StructuralChangesRecorder.End(); // AddComponent
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public void GetAllUniqueSharedComponents_Unmanaged<T>(out UnsafeList<T> sharedComponentValues, AllocatorManager.AllocatorHandle allocator) where T : unmanaged, ISharedComponentData
        {
            EntityComponentStore->GetAllUniqueSharedComponents_Unmanaged<T>(out sharedComponentValues, allocator);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public void GetAllUniqueSharedComponents_Unmanaged<T>(
            out UnsafeList<T> sharedComponentValues,
            out UnsafeList<int> sharedComponentIndices,
            AllocatorManager.AllocatorHandle allocator) where T : unmanaged, ISharedComponentData
        {

            EntityComponentStore->GetAllUniqueSharedComponents_Unmanaged<T>(out sharedComponentValues, out sharedComponentIndices, allocator);
        }

        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public object GetSharedComponentDataNonDefaultBoxed(int shareComponentIndex)
        {
            var typeIndex = Entities.EntityComponentStore.GetComponentTypeFromSharedComponentIndex(shareComponentIndex);
            return EntityComponentStore->GetSharedComponentDataObject_Unmanaged(shareComponentIndex, typeIndex);
        }

        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public object GetSharedComponentDataBoxed(int shareComponentIndex, TypeIndex typeIndex)
        {
            return EntityComponentStore->GetSharedComponentDataObject_Unmanaged(shareComponentIndex, typeIndex);
        }

        public JobHandle GetCreatedAndDestroyedEntitiesAsync(NativeList<int> state, NativeList<Entity> createdEntities,
            NativeList<Entity> destroyedEntities)
        {
            var jobHandle = Entities.EntityComponentStore.GetCreatedAndDestroyedEntities(EntityComponentStore, state,
                createdEntities, destroyedEntities, true);
            DependencyManager->AddDependency(null, 0, null, 0, jobHandle);

            return jobHandle;
        }

        public void GetCreatedAndDestroyedEntities(NativeList<int> state, NativeList<Entity> createdEntities,
            NativeList<Entity> destroyedEntities)
        {
            Entities.EntityComponentStore.GetCreatedAndDestroyedEntities(EntityComponentStore, state, createdEntities,
                destroyedEntities, false);
        }



        [BurstCompile]
        private static void SetSharedComponentDataIndexWithBurst(EntityComponentStore* ecs, in Entity entity,
            in ComponentType componentType, int newSharedComponentDataIndex)
        {
            ecs->SetSharedComponentDataIndex(entity, componentType, newSharedComponentDataIndex);
        }

        /// <summary>
        /// Sets an unmanaged shared component on an entity.
        /// </summary>
        /// <remarks>NOTE: This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).</remarks>
        /// <param name="entity">The target entity.</param>
        /// <param name="typeIndex">The type of the component.</param>
        /// <param name="hashCode">The new hash code of the component.</param>
        /// <param name="componentDataAddr">The unmanaged component data pointer.</param>
        [GenerateTestsForBurstCompatibility]
        public void SetSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(
            Entity entity,
            TypeIndex typeIndex,
            int hashCode,
            void* componentDataAddr)
        {
            UnityEngine.Assertions.AssertCore.IsTrue(TypeManager.IsSharedComponentType(typeIndex));
            var type = ComponentType.FromTypeIndex(typeIndex);
            EntityComponentStore->AssertEntityHasComponent(entity, type);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);

            var newSharedComponentDataIndex = 0;
            if (componentDataAddr != null) // null means default
            {
                newSharedComponentDataIndex = EntityComponentStore->InsertSharedComponent_Unmanaged(typeIndex, hashCode, componentDataAddr, null);
            }

            SetSharedComponentDataIndexWithBurst(EntityComponentStore, entity, type, newSharedComponentDataIndex);


            m_SharedComponentReferenceIndexList.Add(newSharedComponentDataIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        [GenerateTestsForBurstCompatibility]
        public void SetSharedComponentDataAddrDefaultMustBeNullDuringStructuralChange(
            NativeArray<Entity> entities,
            TypeIndex typeIndex,
            int hashCode,
            void* componentDataAddr,
            in SystemHandle originSystem = default)
        {
            var type = ComponentType.FromTypeIndex(typeIndex);
            EntityComponentStore->AssertEntityHasComponent(entities, type);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);

            var newSharedComponentDataIndex = 0;
            if (componentDataAddr != null) // null means default
            {
                newSharedComponentDataIndex = EntityComponentStore->InsertSharedComponent_Unmanaged(typeIndex, hashCode, componentDataAddr, null);
            }

            for (int i = 0; i < entities.Length; i++)
            {
                SetSharedComponentDataIndexWithBurst(EntityComponentStore, entities[i], type, newSharedComponentDataIndex);
            }

            m_SharedComponentReferenceIndexList.Add(newSharedComponentDataIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="newData"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public int InsertSharedComponent_Unmanaged<T>(T newData) where T : unmanaged, ISharedComponentData
        {
            var ti = TypeManager.GetTypeIndex<T>();
            int index;

            var defaultData = default(T);
            index = EntityComponentStore->InsertSharedComponent_Unmanaged(ti,
                0,
                UnsafeUtility.AddressOf(ref newData),
                UnsafeUtility.AddressOf(ref defaultData));

            m_SharedComponentReferenceIndexList.Add(index);
            return index;
        }

        [ExcludeFromBurstCompatTesting("Potentially accesses managed component store")]
        public void AddSharedComponentReference(int sharedComponentIndex, int numRefs = 1)
        {
            EntityComponentStore->AddSharedComponentReference_Unmanaged(sharedComponentIndex, numRefs);
        }

        [ExcludeFromBurstCompatTesting("Potentially accesses managed component store")]
        public void RemoveSharedComponentReference(int sharedComponentIndex, int numRefs = 1)
        {
            EntityComponentStore->RemoveSharedComponentReference_Unmanaged(sharedComponentIndex, numRefs);
        }
        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public int GetSharedComponentCount()
        {
            return EntityComponentStore->GetUnmanagedSharedComponentCount();
        }
        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public NativeParallelHashMap<int, int> MoveAllSharedComponents(EntityComponentStore* srcEntityComponentStore, AllocatorManager.AllocatorHandle allocator)
        {
            var remap = new NativeParallelHashMap<int, int>(srcEntityComponentStore->GetUnmanagedSharedComponentCount(), allocator);
            EntityComponentStore->MoveAllSharedComponents_Unmanaged(srcEntityComponentStore, ref remap);
            return remap;
        }

        [BurstCompile]
        static void BuildSharedComponentMapForMoving(ref NativeHashMap<int, int> hashmap, ref NativeArray<ArchetypeChunk> chunks)
        {
            var map = hashmap;
            // Build a map of all shared component values that will be moved with the appropriate refcount
            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i].m_Chunk;
                var archetype = chunks[i].Archetype.Archetype;
                var sharedComponentValues = archetype->Chunks.GetSharedComponentValues(chunk.ListIndex);

                for (int sharedComponentValueIndex = 0; sharedComponentValueIndex < archetype->NumSharedComponents; ++sharedComponentValueIndex)
                {
                    var sharedComponentIndex = sharedComponentValues[sharedComponentValueIndex];
                    if (map.TryGetValue(sharedComponentIndex, out var refCounter))
                    {
                        map[sharedComponentIndex] = ++refCounter;
                    }
                    else
                    {
                        map.Add(sharedComponentIndex, 1);
                    }
                }
            }
        }

        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public NativeHashMap<int, int> MoveSharedComponents(EntityComponentStore* srcEntityComponentStore, NativeArray<ArchetypeChunk> chunks, AllocatorManager.AllocatorHandle allocator)
        {
            var map = new NativeHashMap<int, int>(64, allocator);
            BuildSharedComponentMapForMoving(ref map, ref chunks);

            // Move all shared components that are being referenced
            var kvps = map.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < kvps.Length; i++)
            {
                var sharedComponentIndex = kvps.Keys[i];
                int dstIndex;

                // * remove refcount based on refcount table
                // * -1 because CloneSharedComponentNonDefault above adds 1 refcount
                int srcRefCount = kvps.Values[i];
                dstIndex = EntityComponentStore->CloneSharedComponentNonDefault(srcEntityComponentStore, sharedComponentIndex);
                EntityComponentStore->AddSharedComponentReference_Unmanaged(dstIndex, srcRefCount - 1);
                srcEntityComponentStore->RemoveSharedComponentReference_Unmanaged(sharedComponentIndex, srcRefCount);
                EntityComponentStore->IncrementSharedComponentVersion_Unmanaged(dstIndex);

                map[sharedComponentIndex] = dstIndex;
            }

            kvps.Dispose();
            return map;
        }

        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public int InsertSharedComponentAssumeNonDefault(TypeIndex typeIndex, int hashCode, object sharedComponent)
        {
            /*
             * this is actually used in hybrid to read unmanaged shared components, but it is NOT called in dotsrt
             */
            var sharedComponentHandle = GCHandle.Alloc(sharedComponent, GCHandleType.Pinned);
            var sharedComponentAddr = (byte*)sharedComponentHandle.AddrOfPinnedObject();

            var index = EntityComponentStore->InsertSharedComponent_Unmanaged(typeIndex, hashCode, sharedComponentAddr, null);
            sharedComponentHandle.Free();

            return index;
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="typeIndex"></param>
        /// <param name="hashCode"></param>
        /// <param name="newData"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        [GenerateTestsForBurstCompatibility]
        public int InsertSharedComponent_Unmanaged(TypeIndex typeIndex, int hashCode, void* newData, void* defaultValue)
        {
            var sharedComponentIndex = EntityComponentStore->InsertSharedComponent_Unmanaged(typeIndex, hashCode, newData, defaultValue);
            if (sharedComponentIndex != 0)
                m_SharedComponentReferenceIndexList.Add(sharedComponentIndex);
            return sharedComponentIndex;
        }

        [GenerateTestsForBurstCompatibility]
        public void SetSharedComponentData_Unmanaged(
            Entity entity,
            TypeIndex typeIndex,
            void* componentData,
            void* componentDataDefaultValue,
            in SystemHandle originSystem = default)
        {
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);

            var newSharedComponentDataIndex = InsertSharedComponent_Unmanaged(typeIndex, 0, componentData, componentDataDefaultValue);
            EntityComponentStore->SetSharedComponentDataIndex(entity, componentType, newSharedComponentDataIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        [GenerateTestsForBurstCompatibility]
        public void SetSharedComponentData_Unmanaged(
            NativeArray<Entity> entities,
            TypeIndex typeIndex,
            void* componentData,
            void* componentDataDefaultValue,
            in SystemHandle originSystem = default)
        {
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            EntityComponentStore->AssertEntityHasComponent(entities, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);

            var newSharedComponentDataIndex = InsertSharedComponent_Unmanaged(typeIndex, 0, componentData, componentDataDefaultValue);
            StructuralChange.SetSharedComponentDataIndexWithBurst(EntityComponentStore, (Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length, componentType,
                newSharedComponentDataIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        [GenerateTestsForBurstCompatibility]
        public void SetSharedComponentData_Unmanaged(
            ArchetypeChunk chunk,
            TypeIndex typeIndex,
            void* componentData,
            void* componentDataDefaultValue,
            in SystemHandle originSystem = default)
        {
            var componentType = ComponentType.FromTypeIndex(typeIndex);
            var archetype = chunk.Archetype.Archetype;
            UnityEngine.Assertions.AssertCore.IsTrue(ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex) >= 0,
                $"Component shared component type {typeIndex} not found on target chunk");

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);

            var newSharedComponentDataIndex = InsertSharedComponent_Unmanaged(typeIndex, 0, componentData, componentDataDefaultValue);
            EntityComponentStore->SetSharedComponentDataIndexForChunk(chunk.m_Chunk, chunk.Archetype.Archetype, componentType, newSharedComponentDataIndex);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="queryImpl"></param>
        /// <param name="sharedComponentIndex"></param>
        /// <param name="componentType"></param>
        /// <param name="componentData"></param>
        /// <param name="originSystem"></param>
        [GenerateTestsForBurstCompatibility]
        public void SetSharedComponentDataOnQueryDuringStructuralChange_Unmanaged(
            EntityQueryImpl* queryImpl,
            int sharedComponentIndex,
            ComponentType componentType,
            void* componentData,
            in SystemHandle originSystem = default)
        {
            Assert.IsTrue(componentType.IsSharedComponent);
            EntityComponentStore->AssertNonEmptyArchetypesHaveComponent(queryImpl->_QueryData->MatchingArchetypes, componentType);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.SetSharedComponent, in m_WorldUnmanaged);
            queryImpl->SyncFilterTypes();
            StructuralChange.SetSharedComponentDataIndexWithBurst(EntityComponentStore,
                queryImpl, componentType, sharedComponentIndex);
            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();

        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public T GetSharedComponentData_Unmanaged<T>(Entity entity) where T : unmanaged, ISharedComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);

            var sharedComponentIndex = EntityComponentStore->GetSharedComponentDataIndex(entity, typeIndex);
            return GetSharedComponentData_Unmanaged<T>(sharedComponentIndex);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public T GetSharedComponentData_Unmanaged<T>(int sharedComponentIndex) where T : unmanaged
        {
            Assert.IsTrue(Entities.EntityComponentStore.IsUnmanagedSharedComponentIndex(sharedComponentIndex));

            var res = default(T);
            if (sharedComponentIndex != 0)
            {
                EntityComponentStore->GetSharedComponentData_Unmanaged(sharedComponentIndex, TypeManager.GetTypeIndex<T>(), UnsafeUtility.AddressOf(ref res));
            }

            return res;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public int GetSharedComponentVersion_Unmanaged<T>(T sharedData) where T : unmanaged
        {
            var ti = TypeManager.GetTypeIndex<T>();
            var res = default(T);
            return EntityComponentStore->GetSharedComponentVersion_Unmanaged(ti, UnsafeUtility.AddressOf(ref sharedData), UnsafeUtility.AddressOf(ref res));
        }

        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public bool AllSharedComponentReferencesAreFromChunks(EntityComponentStore* entityComponentStore)
        {
            var refCounts = new UnsafeParallelHashMap<int, int>(64, Allocator.Temp);
            for (var i = 0; i < entityComponentStore->m_Archetypes.Length; ++i)
            {
                var archetype = entityComponentStore->m_Archetypes.Ptr[i];
                var chunkCount = archetype->Chunks.Count;
                for (int j = 0; j < archetype->NumSharedComponents; ++j)
                {
                    var values = archetype->Chunks.GetSharedComponentValueArrayForType(j);
                    for (var ci = 0; ci < chunkCount; ++ci)
                    {
                        var sharedComponentIndex = values[ci];
                        if (refCounts.TryGetValue(sharedComponentIndex, out var rc))
                        {
                            refCounts[sharedComponentIndex] = rc + 1;
                        }
                        else
                        {
                            refCounts.Add(sharedComponentIndex, 1);
                        }
                    }
                }
            }

            return EntityComponentStore->AllSharedComponentReferencesAreFromChunks(refCounts);
        }

        [ExcludeFromBurstCompatTesting("Accesses managed component store")]
        public void CopySharedComponents(EntityDataAccess* srcEntityDataAccess, int* sharedComponentIndices, int sharedComponentIndicesCount)
        {
            var srcEntityComponentStore = srcEntityDataAccess->EntityComponentStore;
            var dstEntityComponentStore = EntityComponentStore;

            for (var i = 0; i != sharedComponentIndicesCount; i++)
            {
                var sharedComponentIndex = sharedComponentIndices[i];
                if (sharedComponentIndex == 0)
                    continue;

                int dstIndex;
                dstIndex = dstEntityComponentStore->CloneSharedComponentNonDefault(srcEntityComponentStore, sharedComponentIndex);

                sharedComponentIndices[i] = dstIndex;
            }
        }

        public EntityStorageInfo GetStorageInfo(Entity entity)
        {
            EntityComponentStore->AssertEntitiesExist(&entity, 1);

            var entityInChunk = EntityComponentStore->GetEntityInChunk(entity);
            return new EntityStorageInfo
            {
                Chunk = new ArchetypeChunk(entityInChunk.Chunk, EntityComponentStore),
                IndexInChunk = entityInChunk.IndexInChunk
            };
        }

        private void PlaybackCompanionChangesMono()
        {
            CompanionComponentStore.Playback(ref EntityComponentStore->CompanionChangesTracker);
        }

        [BurstDiscard]
        private void PlaybackManagedDirectly(ref bool didTheThing)
        {
            didTheThing = true;
            PlaybackCompanionChangesMono();
        }

        internal void PlaybackCompanionChanges()
        {
            if (EntityComponentStore->CompanionChangesTracker.Empty)
                return;

            bool monoDidIt = false;
            PlaybackManagedDirectly(ref monoDidIt);
            if (monoDidIt)
                return;

            fixed (void* self = &this)
            {
                new FunctionPointer<PlaybackManagedDelegate>(s_ManagedPlaybackTrampoline.Data).Invoke((IntPtr)self);
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(BurstCompatibleBufferElement)},
            CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        public DynamicBuffer<T> GetBuffer<T>(Entity entity, AtomicSafetyHandle safety,
            AtomicSafetyHandle arrayInvalidationSafety, bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();

            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);
            if (!TypeManager.IsBuffer(typeIndex))
            {
                var typeName = typeIndex.ToFixedString();

                // It would be difficult to hit this error since T is constrained to IBufferElementData
                throw new ArgumentException(
                    $"GetBuffer<{typeName}> may not be IComponentData or ISharedComponentData; currently {TypeManager.GetTypeInfo<T>().Category}");
            }

            if (!IsInExclusiveTransaction)
            {
                if (isReadOnly)
                    DependencyManager->CompleteWriteDependency(typeIndex);
                else
                    DependencyManager->CompleteReadAndWriteDependency(typeIndex);
            }

            BufferHeader* header;
            if (isReadOnly)
            {
                header = (BufferHeader*) EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);
            }
            else
            {
                header = (BufferHeader*) EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                    EntityComponentStore->GlobalSystemVersion);
            }

            int internalCapacity = TypeManager.GetTypeInfo(typeIndex).BufferCapacity;

            var useMemoryInit = EntityComponentStore->useMemoryInitPattern != 0;
            byte memoryInitPattern = EntityComponentStore->memoryInitPattern;
            return new DynamicBuffer<T>(header, safety, arrayInvalidationSafety, isReadOnly, useMemoryInit,
                memoryInitPattern, internalCapacity);
        }

        public unsafe TransformRef GetTransformRef(Entity entity, bool isReadOnly = false)
        {
            TypeIndex typeIndex = TypeManager.GetTypeIndex<TransformRef>();
            ComponentSafetyHandles* ptr = &DependencyManager->Safety;
            return GetTransformRefInternal(entity, ptr->GetSafetyHandle(typeIndex, isReadOnly), ptr->GetBufferSafetyHandle(typeIndex), isReadOnly);
        }

        public unsafe TransformRef GetTransformRefInternal(Entity entity, AtomicSafetyHandle safety, AtomicSafetyHandle hierarchySafety, bool isReadOnly = false)
        {
            TypeIndex typeIndex = TypeManager.GetTypeIndex<TransformRef>();
            EntityComponentStore->AssertEntityHasComponent(entity, typeIndex);

            if (!IsInExclusiveTransaction)
            {
                if (isReadOnly)
                {
                    DependencyManager->CompleteWriteDependency(typeIndex);
                }
                else
                {
                    DependencyManager->CompleteReadAndWriteDependency(typeIndex);
                }
            }

            TransformUnion* header;
            if (isReadOnly)
            {
                header = (TransformUnion*)EntityComponentStore->GetComponentDataWithTypeRO(entity, typeIndex);
            }
            else
            {
                header = (TransformUnion*)EntityComponentStore->GetComponentDataWithTypeRW(entity, typeIndex,
                    EntityComponentStore->GlobalSystemVersion);
            }

            return new TransformRef(header, isReadOnly, safety, hierarchySafety);
        }

        public void SetBufferRaw(Entity entity, TypeIndex componentTypeIndex, BufferHeader* tempBuffer, int sizeInChunk,
            in SystemHandle originSystem = default)
        {
            if (!IsInExclusiveTransaction)
                DependencyManager->CompleteReadAndWriteDependency(componentTypeIndex);

            EntityComponentStore->AssertEntityHasComponent(entity, componentTypeIndex);

            EntityComponentStore->SetBufferRaw(entity, componentTypeIndex, tempBuffer, sizeInChunk);
        }

        public EntityArchetype GetEntityAndSimulateArchetype()
        {
            if (!m_EntityAndSimulateOnlyArchetype.Valid)
                m_EntityAndSimulateOnlyArchetype = CreateArchetype((ComponentType*) null, 0, true);

            return m_EntityAndSimulateOnlyArchetype;
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="srcEntity"></param>
        /// <param name="outputEntities"></param>
        /// <param name="count"></param>
        internal void InstantiateInternalDuringStructuralChange(Entity srcEntity, Entity* outputEntities, int count,
            in SystemHandle originSystem = default)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count must be non-negative");
            EntityComponentStore->AssertEntitiesExist(&srcEntity, 1);
            EntityComponentStore->AssertCanInstantiateEntities(srcEntity);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in m_WorldUnmanaged);

            StructuralChange.InstantiateEntity(EntityComponentStore, &srcEntity, outputEntities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        /// <summary>
        /// This function must be wrapped in BeginStructuralChanges() and EndStructuralChanges(ref EntityComponentStore.ArchetypeChanges changes).
        /// </summary>
        /// <param name="srcEntity"></param>
        /// <param name="outputEntities"></param>
        /// <param name="count"></param>
        internal void InstantiateExistingInternalDuringStructuralChange(Entity srcEntity, Entity* outputEntities, int count,
            in SystemHandle originSystem = default)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count must be non-negative");
            EntityComponentStore->AssertEntitiesExist(&srcEntity, 1);
            EntityComponentStore->AssertCanInstantiateEntities(srcEntity);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in m_WorldUnmanaged);

            StructuralChange.InstantiateExistingEntities(EntityComponentStore, &srcEntity, outputEntities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        internal void InstantiateInternalDuringStructuralChange(Entity* srcEntities, Entity* outputEntities, int count,
            int outputCount, bool removePrefab, in SystemHandle originSystem = default)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count must be non-negative");
            AssertCountsMatch(count, outputCount);
            EntityComponentStore->AssertEntitiesExist(srcEntities, count);
            EntityComponentStore->AssertCanInstantiateEntities(srcEntities, count, removePrefab);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in m_WorldUnmanaged);

            StructuralChange.InstantiateEntities(EntityComponentStore, srcEntities, outputEntities, count, removePrefab);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }

        internal void AllocateAndAssignChunksToExistingEntitiesInstantiate(Entity srcEntity, Entity* existingEntities, int count,
            in SystemHandle originSystem = default)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException("count must be non-negative");
            EntityComponentStore->AssertEntitiesExist(&srcEntity, 1);
            EntityComponentStore->AssertCanInstantiateEntities(srcEntity);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in m_WorldUnmanaged);

            StructuralChange.InstantiateExistingEntities(EntityComponentStore, &srcEntity, existingEntities, count);

            if (StructuralChangesProfiler.Enabled)
                StructuralChangesRecorder.End();
        }


        public void SwapComponents(ArchetypeChunk leftChunk, int leftIndex, ArchetypeChunk rightChunk, int rightIndex)
        {
            if (leftIndex < 0 || leftIndex >= leftChunk.Count)
                throw new ArgumentOutOfRangeException(
                    $"leftIndex {leftIndex} is out of range for chunk count {leftChunk.Count}");
            if (rightIndex < 0 || rightIndex >= rightChunk.Count)
                throw new ArgumentOutOfRangeException(
                    $"rightIndex {rightIndex} is out of range for chunk count {rightChunk.Count}");
            if (!IsInExclusiveTransaction)
                BeforeStructuralChange();

            var globalSystemVersion = EntityComponentStore->GlobalSystemVersion;

            ChunkDataUtility.SwapComponents(leftChunk.Archetype.Archetype, leftChunk.m_Chunk, leftIndex, rightChunk.Archetype.Archetype, rightChunk.m_Chunk, rightIndex, 1,
                globalSystemVersion, globalSystemVersion);
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(BurstCompatibleBufferElement)})]
        public BufferTypeHandle<T> GetBufferTypeHandle<T>(bool isReadOnly)
            where T : unmanaged, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            return new BufferTypeHandle<T>(
                DependencyManager->Safety.GetSafetyHandleForBufferTypeHandle(typeIndex, isReadOnly),
                DependencyManager->Safety.GetBufferHandleForBufferTypeHandle(typeIndex),
                isReadOnly, EntityComponentStore->GlobalSystemVersion);
        }

        [GenerateTestsForBurstCompatibility]
        public unsafe TransformTypeHandle GetTransformTypeHandle(bool isReadOnly)
        {
            TypeIndex typeIndex = TypeManager.GetTypeIndex<TransformRef>();
            AtomicSafetyHandle safetyHandleForComponentTypeHandle = DependencyManager->Safety.GetSafetyHandleForComponentTypeHandle(typeIndex, isReadOnly);
            // TODO DOTS-10269: This should be a safety handle for the TransformHierarchies referenced by the TransformUnions in this chunk?
            AtomicSafetyHandle hierarchySafety = DependencyManager->Safety.GetBufferHandleForBufferTypeHandle(typeIndex);
            return new TransformTypeHandle(safetyHandleForComponentTypeHandle, hierarchySafety, isReadOnly, EntityComponentStore->GlobalSystemVersion);
        }

        public bool HasBlobReferences(int sharedComponentIndex)
        {
            return TypeManager.GetTypeInfo(
                    Entities.EntityComponentStore.GetComponentTypeFromSharedComponentIndex(sharedComponentIndex))
                .HasBlobAssetRefs;
        }

        internal Entity GetEntityByEntityIndex(int index)
        {
            return EntityComponentStore->GetEntityByEntityIndex(index);
        }

        /// <summary>
        /// Gets the name assigned to an entity.
        /// </summary>
        /// <remarks>For performance, entity names only exist when running in the Unity Editor.</remarks>
        /// <param name="entity">The Entity object of the entity of interest.</param>
        /// <returns>The entity name.</returns>
        [ExcludeFromBurstCompatTesting("Returns managed string")]
        public string GetName(Entity entity)
        {
            return EntityComponentStore->GetName(entity);
        }

        public void GetName(Entity entity, out FixedString64Bytes name)
        {
            EntityComponentStore->GetName(entity, out name);
        }

        /// <summary>
        /// Sets the name of an entity.
        /// </summary>
        /// <remarks>For performance, entity names only exist when running in the Unity Editor.</remarks>
        /// <param name="entity">The Entity object of the entity to name.</param>
        /// <param name="name">The name to assign.</param>
        public void SetName(Entity entity, in FixedString64Bytes name)
        {
            EntityComponentStore->SetName(entity, in name);
        }

        /// <summary>
        /// Waits for all tracked jobs to complete.
        /// </summary>
        /// <remarks>Calling <see cref="CompleteAllTrackedJobs"/> blocks the main thread until all currently running tracked Jobs finish.</remarks>
        /// <remarks>Tracked JobHandles for this world include every systems' resulting JobHandle directly after their OnUpdate</remarks>
        public void CompleteAllTrackedJobs()
        {
            DependencyManager->CompleteAllJobsAndInvalidateArrays();
        }
    }
}
