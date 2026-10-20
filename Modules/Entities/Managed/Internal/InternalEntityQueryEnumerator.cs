// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.Internal
{
    /// <summary>
    /// This exists only for internal use and is intended to be only used by source-generated code.
    /// DO NOT USE in user code (this API will change).
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [NoAlias]
    public unsafe struct InternalEntityQueryEnumerator : IDisposable
    {
        // hot state

        /// <summary>
        /// The index of the current entity within its chunk
        /// </summary>
        public int         IndexInChunk;

        /// <summary>
        /// The first entity in the chunk which should not be included in the current hot-loop iteration.
        /// If enabled-bits are in use, this one index past the end of the current entity range.
        /// If not, it will happen to match the number of entities in the current chunk, but this is a coincidence.
        /// </summary>
        int         _EndIndexInChunk;

        // enable bit warm state
        int                _ChunkIndex; // NOTE: _ChunkIndex is cold state, but we want v128 to be 16 byte aligned and there is no reason to waste the space.
        byte               _QueryHasFilterOrEnableable;
        byte               _UseEnableBitsForChunk;
        v128               _EnableBitMask;
        UnsafeChunkCacheIterator      _ChunkCacheIterator;
        UnsafeCachedChunkList _CachedChunkList;
        private int _CachedChunkListLength;

        [NoAlias]
        ComponentDependencyManager* _DependencyManager;
        AtomicSafetyHandle _EntitySafetyHandle;
        private AtomicSafetyHandle _QuerySafetyHandle;

        /// <summary>
        /// Construct an enumerator instance for a query
        /// </summary>
        /// <remarks>
        /// This method includes a sync point on any jobs writing to the query's "filter types"; see
        /// <see cref="EntityQueryImpl.SyncFilterTypes"/> for details.
        /// </remarks>
        /// <param name="query">The query whose matching entities will be enumerated</param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public InternalEntityQueryEnumerator(EntityQuery query)
        {
            IndexInChunk = -1;
            _EndIndexInChunk = 0;

            _UseEnableBitsForChunk = 0;
            _EnableBitMask = default;

            var impl = query._GetImpl();

            _QueryHasFilterOrEnableable = impl->_Filter.RequiresMatchesFilter || impl->_QueryData->HasEnableableComponents == 1 ? (byte)1 : (byte)0;
            if (_QueryHasFilterOrEnableable == 1)
            {
                _ChunkCacheIterator =
                    new UnsafeChunkCacheIterator(impl->_Filter, impl->_QueryData->HasEnableableComponents != 0, impl->GetMatchingChunkCache(), impl->_QueryData->MatchingArchetypes.Ptr);
                _CachedChunkList = default;
                _CachedChunkListLength = -1;

                impl->SyncFilterTypes();
            }
            else
            {
                _ChunkCacheIterator = default;
                _CachedChunkList = impl->GetMatchingChunkCache();
                _CachedChunkListLength = _CachedChunkList.Length;
            }

            _ChunkIndex = -1;
            _DependencyManager = impl->_Access->DependencyManager;
            _DependencyManager->ForEachStructuralChange.BeginIsInForEach(impl);
            _EntitySafetyHandle = _DependencyManager->Safety.GetEntityManagerSafetyHandle();
            _QuerySafetyHandle = query.__safety;
        }

        /// <summary>
        /// Clean up this enumerator instance
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispose()
        {
            AtomicSafetyHandle.CheckReadAndThrow(_QuerySafetyHandle);

            _DependencyManager->ForEachStructuralChange.EndIsInForEach();
        }

        // TODO(DOTS-7398)
        /// <summary>
        /// Attempt to advance to the next entity in the current chunk, updating <see cref="IndexInChunk"/> and <see cref="_EndIndexInChunk"/>.
        /// </summary>
        /// <returns>True if another entity in the current chunk was found, or false if the end of the chunk was reached
        /// (in which case, <see cref="MoveNextColdLoop"/> should be called).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNextHotLoop()
        {
            // This is our hot inner loop, we expect that in the case we want to be fastest,
            // we expect that there are a bunch of entities in one batch to be processed.
            IndexInChunk++;

            if (Hint.Likely(IndexInChunk < _EndIndexInChunk))
                return true;
            return _UseEnableBitsForChunk != 0 && EnabledBitUtility.TryGetNextRange(_EnableBitMask, firstIndexToCheck: _EndIndexInChunk, out IndexInChunk, out _EndIndexInChunk);
        }

        /// <summary>
        /// This method retrieves the next range of entities that fulfill the criteria of the `EntityQuery` passed to the constructor.
        /// If the current chunk has been exhausted, the search is continued in the next chunk (if any).
        /// </summary>
        /// <param name="movedToNewChunk">True if the next range of entities are stored in a different chunk than the previous range. In this case, the new chunk is stored in <paramref name="chunk"/>.</param>
        /// <param name="chunk">The next chunk instance if <paramref name="movedToNewChunk"/> is true; `default(ArchetypeChunk)` is returned otherwise.</param>
        /// <param name="entityStartIndex">The index within the chunk of the first entity in the next range of entities.</param>
        /// <param name="entityCount">The number of entities in the next range.</param>
        /// <returns>True if a new range of entities is found; false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNextEntityRange(out bool movedToNewChunk, out ArchetypeChunk chunk, out int entityStartIndex, out int entityEndIndex)
        {
            if (_QueryHasFilterOrEnableable == 0)
            {
                _ChunkIndex++;

                // If we have iterated through all the chunks
                if (Hint.Unlikely(_ChunkIndex >= _CachedChunkListLength))
                {
                    chunk = default;
                    movedToNewChunk = false;
                    _EndIndexInChunk = 0;
                    entityStartIndex = -1;
                    entityEndIndex = -1;
                    return false;
                }

                chunk = new ArchetypeChunk(_CachedChunkList.ChunkIndices[_ChunkIndex], _CachedChunkList.EntityComponentStore);
                movedToNewChunk = true;
                entityStartIndex = 0;
                entityEndIndex = chunk.Count;
                _EndIndexInChunk = 0;
                return true;
            }

            // If we are checking for enabledness *and* we haven't finished processing the current chunk,
            // then try to find the next range of contiguous bits in the current chunk
            if (_UseEnableBitsForChunk != 0 && EnabledBitUtility.TryGetNextRange(_EnableBitMask, _EndIndexInChunk, out entityStartIndex, out _EndIndexInChunk))
            {
                chunk = default;
                movedToNewChunk = false;
                entityEndIndex = _EndIndexInChunk;
                return true;
            }

            // We have finished processing the current chunk. Are there more we need to process?
            _EndIndexInChunk = 0;
            bool hasChunksLeft = _ChunkCacheIterator.MoveNextChunk(ref _ChunkIndex, out chunk, out int chunkEntityCount, out _UseEnableBitsForChunk, ref _EnableBitMask);

            // If there are no more chunks we need to process
            if (Hint.Unlikely(!hasChunksLeft))
            {
                movedToNewChunk = false;
                entityStartIndex = -1;
                entityEndIndex = -1;
                return false;
            }

            // If the next chunk does not require us to check for enabledness
            if (Hint.Likely(_UseEnableBitsForChunk == 0))
            {
                movedToNewChunk = true;
                entityStartIndex = 0;
                entityEndIndex = chunkEntityCount;
                return true;
            }

            // If the next chunk requires us to check for enabledness, and we can find a new range of contiguous bits
            movedToNewChunk = Hint.Likely(EnabledBitUtility.TryGetNextRange(_EnableBitMask, _EndIndexInChunk, out entityStartIndex, out _EndIndexInChunk));
            entityEndIndex = _EndIndexInChunk;
            return movedToNewChunk;
        }

        // TODO(DOTS-7398)
        /// <summary>
        /// Attempt to advance to the first entity of the next chunk that matches the query, updating <see cref="IndexInChunk"/>
        /// and <see cref="_EndIndexInChunk"/>.
        /// </summary>
        /// <param name="chunk">If successful, the new chunk's metadata is stored here.</param>
        /// <returns>True if a new non-empty matching chunk was found (in which case iteration can continue immediately),
        /// or false if the end of the matching chunk list was reached (in which case iteration should terminate).</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool MoveNextColdLoop(out ArchetypeChunk chunk)
        {
            CheckDisposed();
            if (_QueryHasFilterOrEnableable == 0)
            {
                _ChunkIndex++;

                if (Hint.Unlikely(_ChunkIndex >= _CachedChunkList.Length))
                {
                    chunk = default;
                    return false;
                }

                chunk = new ArchetypeChunk(_CachedChunkList.ChunkIndices[_ChunkIndex], _CachedChunkList.EntityComponentStore);
                IndexInChunk = 0;
                _EndIndexInChunk = chunk.Count;
                return true;
            }

            bool hasChunksLeft = _ChunkCacheIterator.MoveNextChunk(ref _ChunkIndex, out chunk, out int chunkEntityCount, out _UseEnableBitsForChunk, ref _EnableBitMask);

            // Should we setup indices so it's clear we are done???
            if (Hint.Unlikely(!hasChunksLeft))
                return false;

            if (Hint.Unlikely(_UseEnableBitsForChunk != 0))
            {
                _EndIndexInChunk = 0;
                EnabledBitUtility.TryGetNextRange(_EnableBitMask, firstIndexToCheck: _EndIndexInChunk, out IndexInChunk, out _EndIndexInChunk);
            }
            else
            {
                IndexInChunk = 0;
                _EndIndexInChunk = chunkEntityCount;
            }

            return true;
        }

        /// <summary>
        /// Debug method to ensure that the enumerator is in a valid state
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the enumerator has been disposed, or has outlived its validity.</exception>
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void CheckDisposed()
        {
            AtomicSafetyHandle.CheckReadAndThrow(_EntitySafetyHandle);
            AtomicSafetyHandle.CheckWriteAndThrow(_QuerySafetyHandle);

            if (_DependencyManager->ForEachStructuralChange.Depth == 0)
                throw new ObjectDisposedException("The EntityQueryEnumerator has been disposed or has been incorrectly kept alive across System.Update calls.");
        }
    }
}
