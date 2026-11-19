// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace Unity.Entities
{
    [NativeHeader("Modules/Entities/Public/EntityComponentStore.bindings.h")]
    internal static unsafe class EntityComponentStoreBindings
    {
        [FreeFunction("EntityComponentStoreBindings::CheckManagedLayout", IsThreadSafe = true)]
        static extern int CheckManagedLayout(int* values, int count);

        [FreeFunction("EntityComponentStoreBindings::RegisterChunkStore", IsThreadSafe = true)]
        static extern void RegisterChunkStore(void* chunkStore);

        internal static void Register()
        {
            RegisterChunkStore(UnsafeUtility.AddressOf(ref EntityComponentStore.s_chunkStore.Data));
        }

        // Sends the values in the order EntityComponentStoreBindings::CheckManagedLayout compares them, and returns the
        // index of the first one that differs, or -1. NativeLayoutTests calls it.
        internal static int FindLayoutMismatch()
        {
            Span<int> values = stackalloc int[32];
            var i = 0;

            values[i++] = sizeof(ArchetypeChunkData);
            ArchetypeChunkData.GetFieldOffsets(out values[i], out values[i + 1], out values[i + 2], out values[i + 3], out values[i + 4]);
            i += 5;

            var archetype = default(Archetype);
            var archetypeStart = (byte*)&archetype;
            values[i++] = (int)((byte*)&archetype.Chunks - archetypeStart);
            values[i++] = (int)((byte*)&archetype.Types - archetypeStart);
            values[i++] = (int)((byte*)&archetype.Offsets - archetypeStart);
            values[i++] = (int)((byte*)&archetype.SizeOfs - archetypeStart);
            values[i++] = (int)((byte*)&archetype.TypesCount - archetypeStart);

            values[i++] = sizeof(ChunkIndex);
            values[i++] = sizeof(ComponentTypeInArchetype);
            values[i++] = sizeof(TypeIndex);
            values[i++] = sizeof(Entity);
            values[i++] = TypeManager.ClearFlagsMask;
            values[i++] = TypeManager.BufferComponentTypeFlag;
            values[i++] = TypeManager.SharedComponentTypeFlag;
            values[i++] = TypeManager.ManagedComponentTypeFlag;
            values[i++] = TypeManager.ChunkComponentTypeFlag;
            values[i++] = TypeManager.ZeroSizeInChunkTypeFlag;
            ref var chunkStore = ref EntityComponentStore.s_chunkStore.Data;
            values[i++] = (int)((byte*)chunkStore.Megachunks - (byte*)UnsafeUtility.AddressOf(ref chunkStore));
            values[i++] = EntityComponentStore.ChunkStore.Log2ChunksPerMegachunk;
            values[i++] = EntityComponentStore.ChunkStore.Log2MegachunksInUniverse;
            values[i++] = EntityComponentStore.ChunkStore.Log2ChunkSize;
            values[i++] = Chunk.kBufferOffset;

            fixed (int* pointer = values)
            {
                return CheckManagedLayout(pointer, i);
            }
        }
    }
}
