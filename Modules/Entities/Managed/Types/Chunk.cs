// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// #define ENABLE_UNITY_CHUNK_METADATA_ACCESSOR_COUNTERS

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using Assert = UnityEngine.Assertions.AssertCore;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    [Flags]
    internal enum ChunkFlags
    {
        None = 0,
        Unused0 = 1 << 0,
        Unused1 = 1 << 1,
        TempAssertWillDestroyAllInLinkedEntityGroup = 1 << 2
    }

    [DebuggerTypeProxy(typeof(ChunkIndexDebugProxy))]
    struct ChunkIndex : IComparable<ChunkIndex>
    {

        int Value;

        public static ChunkIndex Null => new();

        public ChunkIndex(int value) => Value = value;
        public static implicit operator int(ChunkIndex index) => index.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe Chunk* GetPtr() => EntityComponentStore.s_chunkStore.Data.GetChunkPointer(Value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool MatchesFilter(MatchingArchetype* match, ref EntityQueryFilter filter)
            => match->ChunkMatchesFilter(ListIndex, ref filter);

        public static bool operator ==(ChunkIndex a, ChunkIndex b) => a.Value == b.Value;
        public static bool operator !=(ChunkIndex a, ChunkIndex b) => !(a == b);

        public static bool operator <(ChunkIndex a, ChunkIndex b) => a.Value < b.Value;
        public static bool operator >(ChunkIndex a, ChunkIndex b) => a.Value > b.Value;

        public bool Equals(ChunkIndex other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ChunkIndex other && Equals(other);
        public override int GetHashCode() => Value;

        internal ulong SequenceNumber
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return GetPtr()->SequenceNumber;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    GetPtr()->SequenceNumber = value;
                }
            }
        }

        internal int ListIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return EntityComponentStore.PerChunkArray.ChunkData[Value].ListIndex;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    EntityComponentStore.PerChunkArray.ChunkData[Value].ListIndex = value;
                }
            }
        }

        internal int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return EntityComponentStore.PerChunkArray.ChunkData[Value].EntityCount;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    EntityComponentStore.PerChunkArray.ChunkData[Value].EntityCount = value;
                }
            }
        }

        internal int ListWithEmptySlotsIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return GetPtr()->ListWithEmptySlotsIndex;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    GetPtr()->ListWithEmptySlotsIndex = value;
                }
            }
        }

        internal Entity MetaChunkEntity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return GetPtr()->metaChunkEntity;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    GetPtr()->metaChunkEntity = value;
                }
            }
        }

        internal unsafe byte* Buffer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return GetPtr()->Buffer;
            }
        }

        internal uint Flags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                unsafe
                {
                    return GetPtr()->Flags;
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                unsafe
                {
                    GetPtr()->Flags = value;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(ChunkIndex other)
        {
            return Value.CompareTo(other.Value);
        }
    }

    class ChunkIndexDebugProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        ChunkIndex m_ChunkIndex;

        ChunkIndexDebugProxy(ChunkIndex chunkIndex)
        {
            m_ChunkIndex = chunkIndex;
        }

        public Entity[] Entities
        {
            get
            {
                unsafe
                {
                    if (m_ChunkIndex == ChunkIndex.Null)
                    {
                        return null;
                    }

                    var buffer = (Entity*)m_ChunkIndex.Buffer;
                    var count = m_ChunkIndex.Count;
                    var result = new Entity[count];

                    for (int i = 0; i < count; i++)
                    {
                        result[i] = buffer[i];
                    }

                    return result;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct Chunk
    {
        // Chunk header START

        // The following field is only used during serialization and won't contain valid data at runtime.
        // This is part of a larger refactor, and this field will eventually be removed from this struct.
        [FieldOffset(0)]
        public int ArchetypeIndexForSerialization;

        // The following field is only used during serialization and won't contain valid data at runtime.
        // This is part of a larger refactor, and this field will eventually be removed from this struct.
        [FieldOffset(4)]
        public int CountForSerialization;

        [FieldOffset(8)]
        public Entity metaChunkEntity;

        [FieldOffset(16)]
        public int ListWithEmptySlotsIndex;

        // Special chunk behaviors
        [FieldOffset(20)]
        public uint Flags;

        // SequenceNumber is a unique number for each chunk, across all worlds. (Chunk* is not guranteed unique, in particular because chunk allocations are pooled)
        [FieldOffset(kSerializedHeaderSize)]
        public ulong SequenceNumber;

        // NOTE: SequenceNumber is not part of the serialized header.
        //       It is cleared on write to disk, it is a global in memory sequence ID used for comparing chunks.
        public const int kSerializedHeaderSize = 24;

        // Chunk header END

        // Component data buffer
        // This is where the actual chunk data starts.
        // It's declared like this so we can skip the header part of the chunk and just get to the data.
        public const int kBufferOffset = 64; // (must be cache line aligned)
        [FieldOffset(kBufferOffset)]
        public fixed byte Buffer[4];

        public const int kChunkSize = 16 * 1024;
        public const int kBufferSize = kChunkSize - kBufferOffset;
        public const int kMaximumEntitiesPerChunk = TypeManager.MaximumChunkCapacity;

        public const int kChunkBufferSize = kChunkSize - kBufferOffset;
    }
}
