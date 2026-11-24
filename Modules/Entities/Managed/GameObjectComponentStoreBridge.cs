// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    static unsafe partial class GameObjectComponentStoreBridge
    {
        // Marks archetypes whose entity ids are engine-owned: destroy/dispose detaches the
        // entity from its chunk instead of releasing the id back to the store.
        public struct IsGameObject : IComponentData {}

        // Every GameObject's entity has these, and Simulate, which CreateArchetype adds
        internal const int k_GameObjectEntityTypeCount = 3;

        internal static void GetGameObjectEntityTypes(ComponentType* types)
        {
            types[0] = ComponentType.FromTypeIndex(TypeManager.GetTypeIndex<IsGameObject>());
            types[1] = ComponentType.FromTypeIndex(TypeManager.GetTypeIndex<TransformRef>());
            types[2] = ComponentType.FromTypeIndex(TypeManager.GetTypeIndex<HasHierarchy>());
        }

        // Native code leaves them out of a GameObject's data components
        internal static void RegisterGameObjectEntityTypes()
        {
            var types = stackalloc ComponentType[k_GameObjectEntityTypeCount];
            GetGameObjectEntityTypes(types);
            var typeIds = stackalloc uint[k_GameObjectEntityTypeCount + 1];
            for (var i = 0; i < k_GameObjectEntityTypeCount; ++i)
            {
                typeIds[i] = (uint)types[i].TypeIndex.Index;
            }
            typeIds[k_GameObjectEntityTypeCount] = (uint)TypeManager.GetTypeIndex<Simulate>().Index;
            SetGameObjectEntityTypes(typeIds, k_GameObjectEntityTypeCount + 1);
        }

        // Gives the new GameObject's entity chunk storage in the World on top of the active World stack. A companion
        // GameObject is cloned with its owner entity's id, so its entity already has a chunk.
        [RequiredByNativeCode]
        static void OnGameObjectCreated(ulong entityIdRaw, IntPtr world)
        {
            try
            {
                var entity = UnsafeUtility.As<ulong, Entity>(ref entityIdRaw);
                AttachGameObjectEntity(((WorldUnmanagedImpl*)world)->m_EntityManager.GetCheckedEntityDataAccess(), entity);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [RequiredByNativeCode]
        static bool AttachGameObjectEntityToWorld(IntPtr world, ulong entityIdRaw)
        {
            var entity = UnsafeUtility.As<ulong, Entity>(ref entityIdRaw);
            return AttachGameObjectEntity(((WorldUnmanagedImpl*)world)->m_EntityManager.GetCheckedEntityDataAccess(), entity);
        }

        // Gives an existing engine id a chunk with the GameObject entity archetype, keeping the id. Returns false without
        // changes when the entity store doesn't have the id or the id already has a chunk in any World.
        internal static bool AttachGameObjectEntity(EntityDataAccess* access, Entity entity)
        {
            if (!EntityComponentStore.s_entityStore.Data.Exists(entity))
                return false;
            if (!EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity).Equals(EntityInChunk.Null))
                return false;

            var archetype = access->GetGameObjectEntityArchetype();
            access->PrepareForAdditiveStructuralChanges(archetype.Archetype);
            var changes = access->BeginAdditiveStructuralChanges();
            access->AllocateAndAssignChunksToExistingEntities(archetype, &entity, 1);
            access->EndStructuralChanges(ref changes);
            return true;
        }

        // Writes up to capacity of the GameObject entities the Worlds hold and returns how many they hold.
        [RequiredByNativeCode]
        static int GetGameObjectEntities(ulong* entities, int capacity)
        {
            var count = 0;
            foreach (var world in World.s_AllWorlds)
            {
                var store = world.EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore;
                for (int i = 0, archetypeCount = store->m_Archetypes.Length; i < archetypeCount; i++)
                {
                    var archetype = store->m_Archetypes.Ptr[i];
                    if (!archetype->HasGameObjectEntities)
                        continue;
                    for (int c = 0, chunkCount = archetype->Chunks.Count; c < chunkCount; c++)
                    {
                        var chunk = archetype->Chunks[c];
                        var copied = Math.Min(chunk.Count, Math.Max(capacity - count, 0));
                        UnsafeUtility.MemCpy(entities + count, chunk.Buffer, copied * sizeof(ulong));
                        count += chunk.Count;
                    }
                }
            }
            return count;
        }

        // Gives the GameObject entities that have no chunk storage one in the World, in one structural change.
        [RequiredByNativeCode]
        static void AttachGameObjectEntities(IntPtr world, ulong* entities, int count)
        {
            try
            {
                using var detached = new NativeList<Entity>(count, Allocator.Temp);
                for (var i = 0; i < count; i++)
                {
                    var entity = UnsafeUtility.As<ulong, Entity>(ref entities[i]);
                    if (EntityComponentStore.s_entityStore.Data.Exists(entity) && EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity).Equals(EntityInChunk.Null))
                        detached.Add(entity);
                }
                if (detached.Length == 0)
                    return;

                var access = ((WorldUnmanagedImpl*)world)->m_EntityManager.GetCheckedEntityDataAccess();
                var archetype = access->GetGameObjectEntityArchetype();
                access->PrepareForAdditiveStructuralChanges(archetype.Archetype);
                var changes = access->BeginAdditiveStructuralChanges();
                access->AllocateAndAssignChunksToExistingEntities(archetype, detached.GetUnsafePtr(), detached.Length);
                access->EndStructuralChanges(ref changes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // Detaches a GameObject entity from its chunk so the id release does not leave chunk data behind.
        // A companion shares its id with its entity, so destroying the companion destroys that entity too.
        [RequiredByNativeCode]
        static void OnGameObjectDestroyed(ulong entityIdRaw)
        {
            try
            {
                var entity = UnsafeUtility.As<ulong, Entity>(ref entityIdRaw);
                var chunk = EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity).Chunk;
                if (chunk == ChunkIndex.Null)
                    return;

                var archetype = EntityComponentStore.PerChunkArray.ChunkData[chunk].Archetype;
                if (archetype == null || !archetype->HasGameObjectEntities)
                    return;

                var access = FindEntityDataAccess(archetype->EntityComponentStore);
                if (access == null)
                    return;

                var changes = access->BeginStructuralChanges();
                access->DeallocateAndUnAssignChunksToExistingEntities(&entity, 1);
                access->EndStructuralChanges(ref changes);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        // Companion GameObjects are destroyed during playback, inside the structural change of the store that owns
        // them. A baked companion has an entity of its own, which leaves its chunk here, in that open structural change
        // when it is in the same store, so the destroy callback finds nothing left to detach.
        internal static void DetachGameObjectEntitiesDuringStructuralChange(EntityId* gameObjectIds, int count, EntityComponentStore* changingStore)
        {
            for (int i = 0; i < count; i++)
            {
                var entity = UnsafeUtility.As<EntityId, Entity>(ref gameObjectIds[i]);
                if (!EntityComponentStore.s_entityStore.Data.Exists(entity))
                    continue;
                var chunk = EntityComponentStore.s_entityStore.Data.GetEntityInChunk(entity).Chunk;
                if (chunk == ChunkIndex.Null)
                    continue;

                var archetype = EntityComponentStore.PerChunkArray.ChunkData[chunk].Archetype;
                if (archetype == null || !archetype->HasGameObjectEntities)
                    continue;

                var access = FindEntityDataAccess(archetype->EntityComponentStore);
                if (access == null)
                    continue;

                if (archetype->EntityComponentStore == changingStore)
                {
                    access->DeallocateAndUnAssignChunksToExistingEntities(&entity, 1);
                    continue;
                }

                var changes = access->BeginStructuralChanges();
                access->DeallocateAndUnAssignChunksToExistingEntities(&entity, 1);
                access->EndStructuralChanges(ref changes);
            }
        }

        static EntityDataAccess* FindEntityDataAccess(EntityComponentStore* store)
        {
            var defaultWorld = World.DefaultGameObjectInjectionWorld;
            if (defaultWorld != null && defaultWorld.IsCreated)
            {
                var access = defaultWorld.EntityManager.GetCheckedEntityDataAccess();
                if (access->EntityComponentStore == store)
                    return access;
            }

            foreach (var world in World.s_AllWorlds)
            {
                var access = world.EntityManager.GetCheckedEntityDataAccess();
                if (access->EntityComponentStore == store)
                    return access;
            }

            return null;
        }

        // Creating a GameObject creates an entity, so it can't happen while iterating a query that matches GameObject entities.
        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static void CheckGameObjectCreationAllowed()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return;

            var access = world.EntityManager.GetCheckedEntityDataAccess();
            if (!access->DependencyManager->ForEachStructuralChange.IsAdditiveStructuralChangePossible(access->GetGameObjectEntityArchetype().Archetype))
                throw new InvalidOperationException(
                    "GameObjects can't be created while iterating over entities with a query that matches GameObject entities. " +
                    "Create them after the loop, or narrow the query so it excludes GameObject entities.");
        }
    }
}
