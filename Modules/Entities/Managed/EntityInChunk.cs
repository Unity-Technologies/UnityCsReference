// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities
{
    /// <summary>
    /// Represents a single entity within a chunk. Mainly used internally to sort lists of entities into chunk order.
    /// </summary>
    public struct EntityInChunk : IComparable<EntityInChunk>, IEquatable<EntityInChunk>
    {
        internal ChunkIndex Chunk;
        internal int IndexInChunk;

        /// <summary>
        /// Null is a special value that does not refer to an actual entity. The chunk index it contains is ChunkIndex.Null.
        /// </summary>
        public static EntityInChunk Null => default;

        /// <summary>
        /// Compares two <see cref="EntityInChunk"/> objects to determine their relative ordering
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>-1 if this entity should be ordered earlier than <paramref name="other"/>. 1 if this entity should
        /// be ordered later than <paramref name="other"/>. 0 if the two entities are equivalent.</returns>
        public int CompareTo(EntityInChunk other)
        {
            int chunkCompare = Chunk < other.Chunk ? -1 : 1;
            int indexCompare = IndexInChunk - other.IndexInChunk;
            return Chunk != other.Chunk ? chunkCompare : indexCompare;
        }

        /// <summary>
        /// Compares two <see cref="EntityInChunk"/> instances for equality/
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>True if the two instances refer to the same entity in the same chunk.</returns>
        public bool Equals(EntityInChunk other)
        {
            return CompareTo(other) == 0;
        }
    }
}
