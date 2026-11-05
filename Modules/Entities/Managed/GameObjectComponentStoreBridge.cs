// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Scripting;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    static unsafe partial class GameObjectComponentStoreBridge
    {
        // Marks archetypes whose entity ids are engine-owned: destroy/dispose detaches the
        // entity from its chunk instead of releasing the id back to the store.
        public struct IsGameObject : IComponentData {}

        // Callbacks that fire while a structural change is in flight are deferred here.
        // Destroyed ids are recycled by the engine immediately, so destroys record the chunk
        // to scavenge rather than the (unresolvable) entity.
        [AutoStaticsCleanupOnCodeReload]
        static List<Entity> s_PendingCreated = new List<Entity>();
        [AutoStaticsCleanupOnCodeReload]
        static List<ChunkIndex> s_PendingStaleChunks = new List<ChunkIndex>();
        [AutoStaticsCleanupOnCodeReload]
        static bool s_ProcessPending;

        // True inside a foreach-iteration window or while managed deferred commands are being
        // played back (where companion GameObjects get created/destroyed) - structural changes
        // are not allowed there, so callbacks must defer.
        static bool IsStructuralChangeInProgress(EntityDataAccess* access)
        {
            return access->EntityComponentStore->CompanionChangesTracker.CommandBuffer.Length > 0;
        }

        // Gives the new GameObject's entity chunk storage in the World on top of the active World stack.
        [RequiredByNativeCode]
        static void OnGameObjectCreated(ulong entityIdRaw, IntPtr world)
        {
            try
            {
                var entity = UnsafeUtility.As<ulong, Entity>(ref entityIdRaw);
                if (!EntityComponentStore.s_entityStore.Data.Exists(entity))
                    return;

                var access = ((WorldUnmanagedImpl*)world)->m_EntityManager.GetCheckedEntityDataAccess();
                if (IsStructuralChangeInProgress(access))
                {
                    s_PendingCreated.Add(entity);
                    return;
                }

                ProcessPending(access);
                AssignChunkStorage(access, entity);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // Detaches the entity from its chunk so the id release does not leave chunk data behind.
        [RequiredByNativeCode]
        static void OnGameObjectDestroyed(ulong entityIdRaw)
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return;

                var entity = UnsafeUtility.As<ulong, Entity>(ref entityIdRaw);
                var chunk = EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity).Chunk;
                if (chunk == ChunkIndex.Null)
                    return;

                var access = world.EntityManager.GetCheckedEntityDataAccess();

                // Only the default World is resolved for now.
                var archetype = EntityComponentStore.PerChunkArray.ChunkData[chunk].Archetype;
                if (archetype == null || archetype->EntityComponentStore != access->EntityComponentStore)
                    return;

                if (IsStructuralChangeInProgress(access))
                {
                    s_PendingStaleChunks.Add(chunk);
                    return;
                }

                ProcessPending(access);

                var changes = access->BeginStructuralChanges();
                access->DeallocateAndUnAssignChunksToExistingEntities(&entity, 1);
                access->EndStructuralChanges(ref changes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        static void AssignChunkStorage(EntityDataAccess* access, Entity entity)
        {
            if (!EntityComponentStore.s_entityStore.Data.Exists(entity))
                return;
            if (!EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity).Equals(EntityInChunk.Null))
                return;

            var archetype = access->GetGameObjectEntityArchetype();
            access->PrepareForAdditiveStructuralChanges(archetype.Archetype);
            var changes = access->BeginAdditiveStructuralChanges();
            access->AllocateAndAssignChunksToExistingEntities(archetype, &entity, 1);
            // A deferred entity gets its chunk after its Transform was set up, so its TransformRef is still unassigned
            CoreModuleLightShim.Hooks.AssignTransformHierarchyToGameObjectEntity(entity);
            access->EndStructuralChanges(ref changes);
        }

        // Called from EntityDataAccess.EndStructuralChanges so deferred work resolves in the
        // same structural-change scope that produced it.
        internal static void ProcessPendingAfterStructuralChange()
        {
            if (s_ProcessPending || (s_PendingCreated.Count == 0 && s_PendingStaleChunks.Count == 0))
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var access = world.EntityManager.GetCheckedEntityDataAccess();
            if (IsStructuralChangeInProgress(access))
                return;

            ProcessPending(access);
        }

        static void ProcessPending(EntityDataAccess* access)
        {
            if (s_ProcessPending)
                return;

            s_ProcessPending = true;
            try
            {
                while (s_PendingStaleChunks.Count > 0 || s_PendingCreated.Count > 0)
                {
                    if (s_PendingStaleChunks.Count > 0)
                    {
                        var chunk = s_PendingStaleChunks[s_PendingStaleChunks.Count - 1];
                        s_PendingStaleChunks.RemoveAt(s_PendingStaleChunks.Count - 1);
                        ScavengeStaleEntities(access, chunk);
                    }
                    else
                    {
                        var entity = s_PendingCreated[s_PendingCreated.Count - 1];
                        s_PendingCreated.RemoveAt(s_PendingCreated.Count - 1);
                        AssignChunkStorage(access, entity);
                    }
                }
            }
            finally
            {
                s_ProcessPending = false;
            }
        }

        // Removes chunk entries whose GameObject died while destruction was deferred: the id is
        // already recycled, so stale entries are found by scanning the recorded chunk.
        static void ScavengeStaleEntities(EntityDataAccess* access, ChunkIndex chunk)
        {
            var archetype = EntityComponentStore.PerChunkArray.ChunkData[chunk].Archetype;
            if (archetype == null
                || !archetype->HasGameObjectEntities
                || archetype->EntityComponentStore != access->EntityComponentStore)
                return;

            var changes = access->BeginStructuralChanges();
            for (int i = chunk.Count - 1; i >= 0; i--)
            {
                var entity = ((Entity*)chunk.Buffer)[i];
                if (EntityComponentStore.s_entityStore.Data.Exists(entity))
                    continue;

                access->EntityComponentStore->DestroyStaleGameObjectEntityBatch(
                    new EntityBatchInChunk { Chunk = chunk, StartIndex = i, Count = 1 });
            }
            access->EndStructuralChanges(ref changes);
        }
    }
}
