// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities
{
    static unsafe partial class EntityDiffer
    {
#pragma warning disable 649
        internal struct CreatedEntity
        {
            public EntityGuid EntityGuid;
            public EntityInChunk AfterEntityInChunk;
        }

        internal struct DestroyedEntity
        {
            public EntityGuid EntityGuid;
            public EntityInChunk BeforeEntityInChunk;
        }

        internal struct ModifiedEntity
        {
            public EntityGuid EntityGuid;
            public EntityInChunk BeforeEntityInChunk;
            public EntityInChunk AfterEntityInChunk;
            public bool CanCompareChunkVersions;
        }

        internal struct NameModifiedEntity
        {
            public EntityGuid EntityGuid;
            public Entity Entity;
        }
#pragma warning restore 649

        readonly struct EntityInChunkChanges : IDisposable
        {
            public readonly EntityManager AfterEntityManager;
            public readonly EntityManager BeforeEntityManager;
            public readonly NativeList<CreatedEntity> CreatedEntities;
            public readonly NativeList<ModifiedEntity> ModifiedEntities;
            public readonly NativeList<DestroyedEntity> DestroyedEntities;
            public readonly NativeList<NameModifiedEntity> NameModifiedEntities;

            public readonly bool IsCreated;

            public EntityInChunkChanges(
                EntityManager afterEntityManager,
                EntityManager beforeEntityManager,
                AllocatorManager.AllocatorHandle allocator)
            {
                AfterEntityManager = afterEntityManager;
                BeforeEntityManager = beforeEntityManager;
                CreatedEntities = new NativeList<CreatedEntity>(16, allocator);
                ModifiedEntities = new NativeList<ModifiedEntity>(16, allocator);
                DestroyedEntities = new NativeList<DestroyedEntity>(16, allocator);
                NameModifiedEntities = new NativeList<NameModifiedEntity>(16, allocator);
                IsCreated = true;
            }

            public void Dispose()
            {
                CreatedEntities.Dispose();
                ModifiedEntities.Dispose();
                DestroyedEntities.Dispose();
                NameModifiedEntities.Dispose();
            }
        }

        internal struct EntityInChunkWithGuid
        {
            public EntityInChunk EntityInChunk;
            public EntityGuid EntityGuid;
            public Entity Entity;
            public int NameIndex;
            public ArchetypeChunkChangeFlags Flags;
        }

        struct EntityInChunkWithGuidComparer : IComparer<EntityInChunkWithGuid>
        {
            public int Compare(EntityInChunkWithGuid x, EntityInChunkWithGuid y) => x.EntityGuid.CompareTo(y.EntityGuid);
        }

        [BurstCompile]
        internal struct GatherEntityInChunkWithGuid : IJobParallelFor
        {
            public TypeIndex EntityGuidTypeIndex;
            public TypeIndex EntityTypeIndex;
            [ReadOnly] public NativeList<ArchetypeChunk> Chunks;
            [ReadOnly] public NativeList<ArchetypeChunkChangeFlags> Flags;
            [ReadOnly] public NativeList<int> EntityCounts;
            public EntityNameStoreAccess NameStoreAccess;

            [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<EntityInChunkWithGuid> Entities;

            public void Execute(int index)
            {
                var chunk = Chunks[index].m_Chunk;
                var flags = Flags[index];
                var startIndex = EntityCounts[index];
                var chunkBuffer = chunk.Buffer;

                var archetype = Chunks[index].Archetype.Archetype;
                var entityGuidIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, EntityGuidTypeIndex);
                var entityGuidBuffer = (EntityGuid*)(chunkBuffer + archetype->Offsets[entityGuidIndexInArchetype]);
                var entityIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, EntityTypeIndex);
                var entityBuffer = (Entity*)(chunkBuffer + archetype->Offsets[entityIndexInArchetype]);

                var entitiesIndex = startIndex;
                for (int i = 0, count = chunk.Count; i < count; ++i)
                {
                    var entity = entityBuffer[i];
                    int nameIndex = 0;


                    nameIndex = EntityNameStorage.GetEntityName(entity).Index;

                    Entities[entitiesIndex++] = new EntityInChunkWithGuid
                    {
                        EntityInChunk = new EntityInChunk { Chunk = chunk, IndexInChunk = i },
                        EntityGuid = entityGuidBuffer[i],
                        Entity = entityBuffer[i],
                        NameIndex = nameIndex,
                        Flags = flags
                    };
                }
            }
        }

        [BurstCompile]
        internal struct SortEntityInChunk : IJob
        {
            public NativeArray<EntityInChunkWithGuid> Array;
            public void Execute() => Array.Sort(new EntityInChunkWithGuidComparer());
        }

        [BurstCompile]
        internal struct GatherEntityChanges : IJob
        {
            [ReadOnly] public NativeArray<EntityInChunkWithGuid> BeforeEntities;
            [ReadOnly] public NativeArray<EntityInChunkWithGuid> AfterEntities;
            [WriteOnly] public NativeList<CreatedEntity> CreatedEntities;
            [WriteOnly] public NativeList<ModifiedEntity> ModifiedEntities;
            [WriteOnly] public NativeList<DestroyedEntity> DestroyedEntities;
            [WriteOnly] public NativeList<NameModifiedEntity> NameModifiedEntities;

            public void Execute()
            {
                var afterEntityIndex = 0;
                var beforeEntityIndex = 0;

                while (beforeEntityIndex < BeforeEntities.Length && afterEntityIndex < AfterEntities.Length)
                {
                    var beforeEntity = BeforeEntities[beforeEntityIndex];
                    var afterEntity = AfterEntities[afterEntityIndex];
                    var compare = beforeEntity.EntityGuid.CompareTo(afterEntity.EntityGuid);

                    if (compare < 0)
                    {
                        DestroyedEntities.Add(new DestroyedEntity
                        {
                            EntityGuid = beforeEntity.EntityGuid,
                            BeforeEntityInChunk = beforeEntity.EntityInChunk
                        });
                        beforeEntityIndex++;
                    }
                    else if (compare == 0)
                    {
                        ModifiedEntities.Add(new ModifiedEntity
                        {
                            EntityGuid = afterEntity.EntityGuid,
                            AfterEntityInChunk = afterEntity.EntityInChunk,
                            BeforeEntityInChunk = beforeEntity.EntityInChunk,
                            CanCompareChunkVersions = (beforeEntity.Flags & afterEntity.Flags & ArchetypeChunkChangeFlags.Cloned) == ArchetypeChunkChangeFlags.Cloned
                        });

                        if(afterEntity.NameIndex != beforeEntity.NameIndex)
                        {
                            NameModifiedEntities.Add(new NameModifiedEntity
                            {
                                EntityGuid = afterEntity.EntityGuid,
                                Entity = afterEntity.Entity
                            });
                        }

                        afterEntityIndex++;
                        beforeEntityIndex++;
                    }
                    else
                    {
                        CreatedEntities.Add(new CreatedEntity
                        {
                            EntityGuid = afterEntity.EntityGuid,
                            AfterEntityInChunk = afterEntity.EntityInChunk
                        });
                        afterEntityIndex++;
                    }
                }

                while (beforeEntityIndex < BeforeEntities.Length)
                {
                    var beforeEntity = BeforeEntities[beforeEntityIndex];
                    DestroyedEntities.Add(new DestroyedEntity
                    {
                        EntityGuid = beforeEntity.EntityGuid,
                        BeforeEntityInChunk = beforeEntity.EntityInChunk
                    });
                    beforeEntityIndex++;
                }

                while (afterEntityIndex < AfterEntities.Length)
                {
                    var afterEntity = AfterEntities[afterEntityIndex];
                    CreatedEntities.Add(new CreatedEntity
                    {
                        EntityGuid = afterEntity.EntityGuid,
                        AfterEntityInChunk = afterEntity.EntityInChunk
                    });
                    afterEntityIndex++;
                }
            }
        }

        static NativeArray<EntityInChunkWithGuid> GetSortedEntitiesInChunk
        (
            EntityManager entityManager,
            ArchetypeChunkChangeSet archetypeChunkChangeSet,
            AllocatorManager.AllocatorHandle allocator,
            out JobHandle jobHandle,
            JobHandle dependsOn = default)
        {
            // Todo: When NativeArray supports custom allocators, remove these .ToAllocator callsites DOTS-7695
            var entities = new NativeArray<EntityInChunkWithGuid>(archetypeChunkChangeSet.TotalEntityCount, allocator.ToAllocator, NativeArrayOptions.UninitializedMemory);

            var gatherEntitiesByChunk = new GatherEntityInChunkWithGuid
            {
                EntityGuidTypeIndex = TypeManager.GetTypeIndex<EntityGuid>(),
                EntityTypeIndex = TypeManager.GetTypeIndex<Entity>(),
                Chunks = archetypeChunkChangeSet.Chunks,
                Flags = archetypeChunkChangeSet.Flags,
                EntityCounts = archetypeChunkChangeSet.EntityCounts,

                NameStoreAccess = entityManager.GetCheckedEntityDataAccess()->EntityComponentStore->NameStoreAccess,

                Entities = entities
            }.Schedule(archetypeChunkChangeSet.Chunks.Length, 64, dependsOn);

            var sortEntities = new SortEntityInChunk
            {
                Array = entities
            }.Schedule(gatherEntitiesByChunk);

            jobHandle = sortEntities;

            return entities;
        }

        static EntityInChunkChanges GetEntityInChunkChanges
        (
            EntityManager afterEntityManager,
            EntityManager beforeEntityManager,
            NativeArray<EntityInChunkWithGuid> afterEntities,
            NativeArray<EntityInChunkWithGuid> beforeEntities,
            AllocatorManager.AllocatorHandle allocator,
            out JobHandle jobHandle,
            JobHandle dependsOn = default)
        {
            var entityChanges = new EntityInChunkChanges
                (
                afterEntityManager,
                beforeEntityManager,
                allocator
                );

            jobHandle = new GatherEntityChanges
            {
                AfterEntities = afterEntities,
                BeforeEntities = beforeEntities,
                CreatedEntities = entityChanges.CreatedEntities,
                ModifiedEntities = entityChanges.ModifiedEntities,
                DestroyedEntities = entityChanges.DestroyedEntities,
                NameModifiedEntities = entityChanges.NameModifiedEntities
            }.Schedule(dependsOn);

            return entityChanges;
        }
    }
}
