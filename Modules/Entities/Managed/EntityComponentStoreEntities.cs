// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;
using Assert = UnityEngine.Assertions.AssertCore;


namespace Unity.Entities
{
    unsafe partial struct EntityComponentStore
    {
        // EntityStore delegates all slot storage, per-block bitmaps, and per-block
        // atomic counts to UnityEngine.EntityIdStore (EntityIdStore.bindings.cs).
        // EntityIdStore's layout fields now live in a SharedStatic so Burst can read
        // them without touching the class static constructor, which contains external
        // calls.  This file is a thin DOTS adapter: it adds chunk-placement tracking
        // and the ECS-specific accessors on top.

        internal static readonly SharedStatic<EntityStore> s_entityStore =
            SharedStatic<EntityStore>.GetOrCreate<EntityStore.BurstStaticIdentifier>();

        internal struct EntityStore : IDisposable
        {
            internal struct BurstStaticIdentifier { }

            internal const int MaximumTheoreticalAmountOfEntities = 1 << Entity.kIndexBits;

            // ── Debug helpers — delegate to EntityIdStore ─────────────────────

            public int DebugOnlyThreadUnsafeEntityCount => EntityIdStore.DebugOnlyThreadUnsafeEntityCount();
            public void IntegrityCheck(int blockIndex)  => EntityIdStore.IntegrityCheck(blockIndex);
            public void IntegrityCheck()                => EntityIdStore.IntegrityCheck();

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            void DebugOnlyThrowIfEntityDoesntExist(Entity entity, EntityIdStore.EntitySlot* slot)
            {
                if (EntityIdStore.SlotGetVersion(Volatile.Read(ref slot->versionAndChunk)) != (uint)entity.Version)
                {
                    throw new ArgumentException(
                        "All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");
                }
            }

            // ── Public accessors ─────────────────────────────────────────────

            internal Entity GetEntityByEntityIndex(int index)
            {
                if ((uint)index >= (uint)MaximumTheoreticalAmountOfEntities) return Entity.Null;
                ref var slot = ref EntityIdStore.GetSlot((uint)index);
                uint version = EntityIdStore.SlotGetVersion(Volatile.Read(ref slot.versionAndChunk));
                if ((version & 1u) == 0) return Entity.Null;
                var entity = default(Entity);
                entity.Index   = index;
                entity.Version = (int)version;
                return entity;
            }

            internal bool Exists(Entity entity)
                => EntityIdStore.Exists(UnsafeUtility.As<Entity, EntityId>(ref entity));

            public EntityInChunk GetEntityInChunk(Entity entity)
            {
                // Version 0 is the null sentinel — no live entity ever carries it.
                // Entity.Index is masked to 28 bits so no bounds check is needed.
                if (entity.Version == 0) return EntityInChunk.Null;
                ref var slot = ref EntityIdStore.GetSlot((uint)entity.Index);
                ulong vac = Volatile.Read(ref slot.versionAndChunk);
                if (EntityIdStore.SlotGetVersion(vac) != (uint)entity.Version) return EntityInChunk.Null;
                return new EntityInChunk
                {
                    Chunk        = new ChunkIndex(EntityIdStore.SlotGetChunkIndex(vac)),
                    IndexInChunk = EntityIdStore.SlotGetIndexInChunk(vac),
                };
            }

            public void SetEntityInChunk(Entity entity, EntityInChunk entityInChunk)
            {
                ref var slot = ref EntityIdStore.GetSlot((uint)entity.Index);
                DebugOnlyThrowIfEntityDoesntExist(entity, (EntityIdStore.EntitySlot*)UnsafeUtility.AddressOf(ref slot));
                Assert.IsTrue((uint)entityInChunk.IndexInChunk < 128u);

                // Must make sure that we maintain existing kind so that GameObject entities in chunk are still correct (kind not cleared)
                int kind = EntityIdStore.SlotGetKind(Volatile.Read(ref slot.versionAndChunk));

                // entity.Version is already validated to match the slot — use it directly
                // rather than reading vac just to extract the same version field.
                Volatile.Write(ref slot.versionAndChunk,
                    EntityIdStore.SlotPackVersionAndChunkAndKind(
                        (uint)entity.Version,
                        (byte)entityInChunk.IndexInChunk,
                        (uint)(int)entityInChunk.Chunk,
                        kind));
            }

            public void SetEntityVersion(Entity entity, int version)
            {
                // DOTS-specific liveness check; core slot write delegates to EntityIdStore.
                var slot = EntityIdStore.GetSlot((uint)entity.Index);
                DebugOnlyThrowIfEntityDoesntExist(entity, (EntityIdStore.EntitySlot*)UnsafeUtility.AddressOf(ref slot));
                EntityIdStore.SetEntityVersion(UnsafeUtility.As<Entity, EntityId>(ref entity), version);
            }

            // ── Allocate — thin casts to EntityIdStore.AllocateEntityIds ─────

            internal void AllocateEntities(NativeArray<Entity> entities)
                => AllocateEntitiesDirect((Entity*)entities.GetUnsafePtr(), entities.Length, ChunkIndex.Null, 0);

            internal void AllocateEntities(Entity* entities, int entityCount)
                => AllocateEntitiesDirect(entities, entityCount, ChunkIndex.Null, 0);

            internal void AllocateEntitiesDirect(Entity* entities, int totalCount, ChunkIndex chunkIndex,
                int firstEntityInChunkIndex)
            {
                bool hasChunk  = chunkIndex != ChunkIndex.Null;
                uint chunkBits = hasChunk ? (uint)(int)chunkIndex : 0u;
                EntityIdStore.AllocateEntityIds(
                    (EntityId*)entities, totalCount,
                    chunkBits, hasChunk ? (byte)firstEntityInChunkIndex : (byte)0);
            }

            // ── Release ───────────────────────────────────────────────────────

            internal void DeallocateEntities(NativeArray<Entity> entities)
                => DeallocateEntities((Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length);

            internal void DeallocateEntities(Entity* entities, int count)
                => EntityIdStore.ReleaseEntityIds((EntityId*)entities, count);

            public void Dispose() { this = default; }
        }
    }
}
