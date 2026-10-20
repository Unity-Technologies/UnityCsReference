// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Unity.Entities
{
    struct DuplicateEntityGuid
    {
        public EntityGuid EntityGuid;
        public int DuplicateCount;

        public DuplicateEntityGuid(EntityGuid entityGuid, int duplicateCount)
        {
            EntityGuid = entityGuid;
            DuplicateCount = duplicateCount;
        }
    }

    static partial class EntityDiffer
    {
        [BurstCompile]
        internal struct BuildEntityGuidToEntity : IJob
        {
            [ReadOnly] public NativeArray<EntityInChunkWithGuid> SortedEntitiesWithGuid;
            [WriteOnly] public NativeList<DuplicateEntityGuid> Duplicates;

            public void Execute()
            {
                if (SortedEntitiesWithGuid.Length == 0)
                    return;

                var previous = SortedEntitiesWithGuid[0].EntityGuid;

                for (var i = 1; i < SortedEntitiesWithGuid.Length; ++i)
                {
                    var current = SortedEntitiesWithGuid[i].EntityGuid;

                    if (current == previous)
                    {
                        var count = 1;
                        for (; ++i < SortedEntitiesWithGuid.Length; ++count)
                        {
                            var testGuid = SortedEntitiesWithGuid[i].EntityGuid;
                            if (testGuid != previous)
                            {
                                --i;
                                break;
                            }
                        }

                        Duplicates.Add(new DuplicateEntityGuid(current, count));
                    }

                    previous = current;
                }
            }
        }

        static NativeList<DuplicateEntityGuid> GetDuplicateEntityGuids(
            NativeArray<EntityInChunkWithGuid> sortedEntitiesWithGuid,
            AllocatorManager.AllocatorHandle allocator,
            out JobHandle jobHandle,
            JobHandle dependsOn = default)
        {
            var duplicates = new NativeList<DuplicateEntityGuid>(1, allocator);

            jobHandle = new BuildEntityGuidToEntity
            {
                SortedEntitiesWithGuid = sortedEntitiesWithGuid,
                Duplicates = duplicates
            }.Schedule(dependsOn);

            return duplicates;
        }
    }
}
