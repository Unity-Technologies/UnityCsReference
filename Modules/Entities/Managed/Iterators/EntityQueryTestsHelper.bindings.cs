// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace Unity.Entities
{
    // Runs native EntityQuery code (Modules/Entities/Public/EntityQuery.h) for tests that compare it with C# queries.
    // A query is passed as a handle, 0 for none. A changed type id of 0 means no change filter.
    [NativeHeader("Modules/Entities/Public/EntityQueryTestsHelper.bindings.h")]
    internal static unsafe class EntityQueryTestsHelper
    {
        [FreeFunction("EntityQueryTestsHelper::CreateQuery")]
        internal static extern ulong CreateQuery(void* world, void* typeIds, void* kinds, int count, int options);

        [FreeFunction("EntityQueryTestsHelper::DestroyQuery")]
        internal static extern void DestroyQuery(ulong query);

        [FreeFunction("EntityQueryTestsHelper::GetCacheEntryIndex")]
        internal static extern int GetCacheEntryIndex(ulong query);

        // Writes each chunk's entity array address and its two enabled mask words, up to capacity, and returns the chunk count
        [FreeFunction("EntityQueryTestsHelper::GetChunks")]
        internal static extern int GetChunks(ulong query, uint changedTypeId, uint requiredVersion, void* outEntityArrays,
            void* outEnabledMasks, int capacity);

        [FreeFunction("EntityQueryTestsHelper::GetEntities")]
        internal static extern int GetEntities(ulong query, uint changedTypeId, uint requiredVersion, void* outEntities, int capacity);

        [FreeFunction("EntityQueryTestsHelper::CalculateEntityCount")]
        internal static extern int CalculateEntityCount(ulong query, uint changedTypeId, uint requiredVersion);

        [FreeFunction("EntityQueryTestsHelper::IsEmpty")]
        internal static extern bool IsEmpty(ulong query, uint changedTypeId, uint requiredVersion);

        // Sets the first int of the component of every matching entity, through GetComponentDataPtrRW, and returns how
        // many it set
        [FreeFunction("EntityQueryTestsHelper::SetFirstInt")]
        internal static extern int SetFirstInt(ulong query, uint typeId, int value, uint changeVersion);

        // Adds up the first int of the component of every matching entity, through GetComponentDataPtrRO
        [FreeFunction("EntityQueryTestsHelper::SumFirstInt")]
        internal static extern long SumFirstInt(ulong query, uint typeId);

        [FreeFunction("EntityQueryTestsHelper::CompleteDependencyBeforeRO")]
        internal static extern void CompleteDependencyBeforeRO(void* world, uint typeId);

        [FreeFunction("EntityQueryTestsHelper::CompleteDependencyBeforeRW")]
        internal static extern void CompleteDependencyBeforeRW(void* world, uint typeId);
    }
}
