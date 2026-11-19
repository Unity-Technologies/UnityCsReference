// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Entities
{
    static partial class EntityQueryProfiler
    {
        // Overloads and the Async variants that return the same kind of result share an operation.
        public enum Operation : int
        {
            IsEmpty,
            CalculateEntityCount,
            CalculateEntityCountWithoutFiltering,
            CalculateChunkCount,
            CalculateChunkCountWithoutFiltering,
            CalculateFilteredChunkIndexArray,
            CalculateFilteredChunkIndexArrayAsync,
            CalculateBaseEntityIndexArray,
            CalculateBaseEntityIndexArrayAsync,
            ToArchetypeChunkArray,
            ToArchetypeChunkList,
            ToArchetypeChunkListAsync,
            ToEntityArray,
            ToEntityListAsync,
            ToComponentDataArray,
            ToComponentDataListAsync,
            CopyFromComponentDataArray,
            CopyFromComponentDataListAsync,
            GetSingletonEntity,
            GetSingleton,
            GetSingletonRW,
            GetSingletonBuffer,
            SetSingleton,
            TryGetSingleton,
            TryGetSingletonRW,
            TryGetSingletonBuffer,
            TryGetSingletonEntity,
            HasSingleton,
            SetEnabledBitsOnAllChunks,
            Foreach,
            ScheduleJob,
            BatchAddComponent,
            BatchRemoveComponent,
            BatchSetSharedComponent,
            BatchDestroyEntity,
        }
    }
}
