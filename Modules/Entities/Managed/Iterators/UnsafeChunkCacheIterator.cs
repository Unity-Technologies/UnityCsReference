// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [NoAlias]
    [GenerateTestsForBurstCompatibility]
    unsafe struct UnsafeChunkCacheIterator
    {
        [NoAlias]
        public int                              Length;

        [NoAlias]
        [NativeDisableUnsafePtrRestriction]
        ChunkIndex*                            _Chunks;

        [NativeDisableUnsafePtrRestriction]
        [NoAlias] EntityComponentStore*        _EntityComponentStore;

        [NoAlias] int*                         _PerChunkMatchingArchetypeIndex;
        [NoAlias] int*                         _ChunkIndexInArchetype;

        [NoAlias] MatchingArchetype**          _MatchingArchetypes;

        EntityQueryFilter                      _Filter;
        byte                                   _RequireMatchesFilter;
        byte                                   _QueryHasEnableableComponents;

        int                                    _CurrentMatchingArchetypeIndex;

        [NoAlias] public MatchingArchetype*    _CurrentMatchingArchetype;
        [NoAlias] int*                         _CurrentArchetypeChunkEntityCounts;
        ChunkIterationUtility.EnabledMaskMatchingArchetypeState _CurrentArchetypeState;

        internal UnsafeChunkCacheIterator(in EntityQueryFilter filter, bool hasEnableableComponents, UnsafeCachedChunkList list, MatchingArchetype** matchingArchetypes)
        {
            _Chunks = list.ChunkIndices;
            Length = list.Length;
            _EntityComponentStore = list.EntityComponentStore;
            _MatchingArchetypes = matchingArchetypes;
            _PerChunkMatchingArchetypeIndex = list.PerChunkMatchingArchetypeIndex->Ptr;
            _ChunkIndexInArchetype = list.ChunkIndexInArchetype->Ptr;
            _Filter = filter;
            _RequireMatchesFilter = filter.RequiresMatchesFilter ? (byte)1 : (byte)0;
            _QueryHasEnableableComponents = hasEnableableComponents ? (byte)1 : (byte)0;
            _CurrentMatchingArchetypeIndex = -1;
            _CurrentMatchingArchetype = null;
            _CurrentArchetypeChunkEntityCounts = null;
            _CurrentArchetypeState = default;
        }

        /// <summary>
        /// Advance to the next non-empty, non-filtered chunk in the cache, if possible.
        /// </summary>
        /// <remarks>It is critical that no structural changes occur while using this iterator! It caches various pointers
        /// to archetype-level data which would be quietly invalidated by structural changes. There are currently no
        /// checks to detect or report this error. Caller beware!</remarks>
        /// <param name="chunkIndexInCache">The index of the current chunk in the iteration. Calling this function will
        /// increment this index. The caller should initialize this value to -1 before the first call.</param>
        /// <param name="outputChunk">If the function is successful, the next chunk in the iteration is stored here.</param>
        /// <param name="outputChunkEntityCount">If the function is successful, the number of entities in
        /// <paramref name="outputChunk"/>is stored here. Note that this count includes all entities in the chunk; for
        /// a count of the enabled entities, use <see cref="EnabledBitUtility.countbits(v128)"/> on the
        /// <paramref name="enableBits"/> mask.</param>
        /// <param name="outputUseEnableBits">When the function returns, a non-zero value here means that at least one
        /// entity in the chunk is disabled according to the requirements of this <see cref="EntityQuery"/>. In this
        /// case, the contents of the <paramref name="enableBits"/> mask should be used to determine which entities
        /// match the query.</param>
        /// <param name="enableBits">If a valid chunk is returned and at least one entity is disabled for the purposes
        /// of this EntityQuery, the combined mask of all entities in the chunk will be stored here. The contents of
        /// this parameter are only valid if <paramref name="outputUseEnableBits"/> is non-zero when the function
        /// returns.</param>
        /// <returns>True if the next chunk was found. False if the end of the cached chunk list was reached (in which
        /// case, <paramref name="outputChunk"/>, <paramref name="outputChunkEntityCount"/> and
        /// <paramref name="outputUseEnableBits"/> will be be default-initialized, and <paramref name="enableBits"/>
        /// will be undefined).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNextChunk(ref int chunkIndexInCache, out ArchetypeChunk outputChunk, out int outputChunkEntityCount, out byte outputUseEnableBits, ref v128 enableBits)
        {
            if (Hint.Likely(chunkIndexInCache + 1 < Length))
            {
                Common.Prefetch(&EntityComponentStore.PerChunkArray.ChunkData[_Chunks[chunkIndexInCache + 1]], Common.ReadWrite.Read);
            }

            chunkIndexInCache++;
            while (chunkIndexInCache < Length)
            {
                var useEnableBits = false;
                int chunkIndexInArchetype = _ChunkIndexInArchetype[chunkIndexInCache];
                if (Hint.Unlikely(_CurrentMatchingArchetypeIndex != _PerChunkMatchingArchetypeIndex[chunkIndexInCache]))
                {
                    _CurrentMatchingArchetypeIndex = _PerChunkMatchingArchetypeIndex[chunkIndexInCache];
                    _CurrentMatchingArchetype = _MatchingArchetypes[_CurrentMatchingArchetypeIndex];
                    _CurrentArchetypeChunkEntityCounts = _CurrentMatchingArchetype->Archetype->Chunks.GetChunkEntityCountArray();
                    if (_QueryHasEnableableComponents != 0)
                    {
                        _CurrentArchetypeState = new ChunkIterationUtility.EnabledMaskMatchingArchetypeState(_CurrentMatchingArchetype);
                    }
                }
                if (_RequireMatchesFilter != 0 && !_CurrentMatchingArchetype->ChunkMatchesFilter(chunkIndexInArchetype, ref _Filter))
                {
                    chunkIndexInCache++;
                    continue;
                }

                if (_QueryHasEnableableComponents != 0)
                {
                    int chunkEntityCount = _CurrentArchetypeChunkEntityCounts[chunkIndexInArchetype];
                    ChunkIterationUtility.GetEnabledMask(chunkIndexInArchetype, chunkEntityCount, _CurrentArchetypeState, out enableBits);
                    if (enableBits.ULong0 == 0 && enableBits.ULong1 == 0)
                    {
                        chunkIndexInCache++;
                        continue;
                    }

                    var enabledCount = EnabledBitUtility.countbits(enableBits);
                    useEnableBits = enabledCount != chunkEntityCount;
                }

                outputChunkEntityCount = _CurrentArchetypeChunkEntityCounts[chunkIndexInArchetype];
                outputUseEnableBits = useEnableBits ? (byte)1 : (byte)0;
                outputChunk = new ArchetypeChunk(_Chunks[chunkIndexInCache], _EntityComponentStore);
                return true;
            }

            outputUseEnableBits = 0;
            outputChunkEntityCount = 0;
            outputChunk = default;
            return false;
        }
    }
}
