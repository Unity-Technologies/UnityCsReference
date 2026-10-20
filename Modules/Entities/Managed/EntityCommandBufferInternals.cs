// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Assertions;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    [StructLayout(LayoutKind.Sequential, Size = (64 > JobsUtility.CacheLineSize) ? 64 : JobsUtility.CacheLineSize)]
    internal unsafe struct EntityCommandBufferChain
    {
        public ECBChunk* m_Tail;
        public ECBChunk* m_Head;
        public ChainCleanup* m_Cleanup;
        public EntityCommandBufferChain* m_NextChain;
        public int m_LastSortKey;
        public bool m_CanBurstPlayback;
        public int m_EntityCount;

        internal static void InitChain(EntityCommandBufferChain* chain, AllocatorManager.AllocatorHandle allocator)
        {
            chain->m_Cleanup =
                (ChainCleanup*)Memory.Unmanaged.Allocate(sizeof(ChainCleanup), sizeof(ChainCleanup), allocator);
            chain->m_Cleanup->BufferCleanupList = null;
            chain->m_Cleanup->EntityArraysCleanupList = null;

            chain->m_Tail = null;
            chain->m_Head = null;
            chain->m_LastSortKey = -1;
            chain->m_NextChain = null;
            chain->m_CanBurstPlayback = true;
            chain->m_EntityCount = 0;
        }
    }

    internal unsafe struct ECBSharedPlaybackState
    {
        public int CommandBufferID;
    }

    internal unsafe struct ECBChainPlaybackState
    {
        public ECBChunk* Chunk;
        public int Offset;
        public int NextSortKey;
        public bool CanBurstPlayback;
    }

    internal unsafe struct ECBPlaybackState
    {
        public ECBChainPlaybackState* ChainPlaybackState;
        public ChunkIndex CurrentChunk;
        public Archetype* CurrentChunkArchetype;
    }

    internal unsafe struct ECBChainHeapElement
    {
        public int SortKey;
        public int ChainIndex;
    }

    internal unsafe struct ECBChainPriorityQueue : IDisposable
    {
        private readonly ECBChainHeapElement* m_Heap;
        private int m_Size;
        private readonly AllocatorManager.AllocatorHandle m_Allocator;
        private static readonly int BaseIndex = 1;

        public ECBChainPriorityQueue(ECBChainPlaybackState* chainStates, int chainStateCount,
            AllocatorManager.AllocatorHandle alloc)
        {
            m_Size = chainStateCount;
            m_Allocator = alloc;
            m_Heap = (ECBChainHeapElement*)Memory.Unmanaged.Allocate((m_Size + BaseIndex) * sizeof(ECBChainHeapElement),
                64, m_Allocator);
            for (int i = m_Size - 1; i >= m_Size / 2; --i)
            {
                m_Heap[BaseIndex + i].SortKey = chainStates[i].NextSortKey;
                m_Heap[BaseIndex + i].ChainIndex = i;
            }

            for (int i = m_Size / 2 - 1; i >= 0; --i)
            {
                m_Heap[BaseIndex + i].SortKey = chainStates[i].NextSortKey;
                m_Heap[BaseIndex + i].ChainIndex = i;
                Heapify(BaseIndex + i);
            }
        }

        public void Dispose()
        {
            Memory.Unmanaged.Free(m_Heap, m_Allocator);
        }

        public bool Empty
        {
            get { return m_Size <= 0; }
        }

        public ECBChainHeapElement Peek()
        {
            //Assert.IsTrue(!Empty, "Can't Peek() an empty heap");
            if (Empty)
            {
                return new ECBChainHeapElement { ChainIndex = -1, SortKey = -1 };
            }

            return m_Heap[BaseIndex];
        }

        public ECBChainHeapElement Pop()
        {
            //Assert.IsTrue(!Empty, "Can't Pop() an empty heap");
            if (Empty)
            {
                return new ECBChainHeapElement { ChainIndex = -1, SortKey = -1 };
            }

            ECBChainHeapElement top = Peek();
            m_Heap[BaseIndex] = m_Heap[m_Size--];
            if (!Empty)
            {
                Heapify(BaseIndex);
            }

            return top;
        }

        public void ReplaceTop(ECBChainHeapElement value)
        {
            if (Empty)
                Assert.IsTrue(false, "Can't ReplaceTop() an empty heap");
            m_Heap[BaseIndex] = value;
            Heapify(BaseIndex);
        }

        private void Heapify(int i)
        {
            // The index taken by this function is expected to be already biased by BaseIndex.
            // Thus, m_Heap[size] is a valid element (specifically, the final element in the heap)
            if (i < BaseIndex || i > m_Size)
                Assert.IsTrue(false, $"heap index {i} is out of range with size={m_Size}");
            ECBChainHeapElement val = m_Heap[i];
            while (i <= m_Size / 2)
            {
                int child = 2 * i;
                if (child < m_Size && (m_Heap[child + 1].SortKey < m_Heap[child].SortKey))
                {
                    child++;
                }

                if (val.SortKey < m_Heap[child].SortKey)
                {
                    break;
                }

                m_Heap[i] = m_Heap[child];
                i = child;
            }

            m_Heap[i] = val;
        }
    }

    /// <summary>
    /// Organized in memory like a single block with Chunk header followed by Size bytes of data.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ECBChunk
    {
        internal int Used;
        internal int Size;
        internal ECBChunk* Next;
        internal ECBChunk* Prev;

        internal int Capacity => Size - Used;

        internal int Bump(int size)
        {
            var off = Used;
            Used += size;
            return off;
        }

        internal int BaseSortKey
        {
            get
            {
                fixed (ECBChunk* pThis = &this)
                {
                    if (Used < sizeof(BasicCommand))
                    {
                        return -1;
                    }

                    var buf = (byte*)pThis + sizeof(ECBChunk);
                    var header = (BasicCommand*)(buf);
                    return header->SortKey;
                }
            }
        }
    }

    internal unsafe struct EntityCommandBufferData
    {
        public EntityCommandBufferChain m_MainThreadChain;
        public EntityCommandBufferChain* m_ThreadedChains;
        public int m_RecordedChainCount;
        public int m_MinimumChunkSize;
        public AllocatorManager.AllocatorHandle m_Allocator;
        public bool m_ShouldPlayback;
        public bool m_DidPlayback;
        public bool m_ForceFullDispose;
        internal static readonly int ALIGN_64_BIT = 8;
        public int m_CommandBufferID;

        internal void InitForParallelWriter()
        {
            if (m_ThreadedChains != null)
                return;

            int maxThreadCount = JobsUtility.ThreadIndexCount;
            int allocSize = sizeof(EntityCommandBufferChain) * maxThreadCount;

            m_ThreadedChains =
                (EntityCommandBufferChain*)Memory.Unmanaged.Allocate(allocSize, JobsUtility.CacheLineSize, m_Allocator);
            UnsafeUtility.MemClear(m_ThreadedChains, allocSize);
            // each thread's chain is lazily initialized inside Reserve() when its first command is written.
        }

        internal void DestroyForParallelWriter()
        {
            if (m_ThreadedChains != null)
            {
                Memory.Unmanaged.Free(m_ThreadedChains, m_Allocator);
                m_ThreadedChains = null;
            }
        }

        internal Entity* CloneEntitiesArray(NativeArray<Entity> entities)
        {
            var output =
                (Entity*)Memory.Unmanaged.Allocate(entities.Length * sizeof(Entity), ALIGN_64_BIT, m_Allocator);
            int i = 0;
            int len = entities.Length;
            for (; i < len; ++i)
            {
                var e = entities[i];
                output[i] = e;
            }

            for (; i < len; ++i)
            {
                output[i] = entities[i];
            }

            return output;
        }

        internal void AddCreateCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity newEntity,
            EntityArchetype archetype)
        {
            var sizeNeeded = Align(sizeof(CreateCommand) + sizeof(Entity), ALIGN_64_BIT);
            var cmd = (CreateCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.CommandType = op;
            cmd->Header.TotalSize = sizeNeeded;
            cmd->Header.SortKey = chain->m_LastSortKey;
            cmd->Archetype = archetype;
            cmd->EntityCount = 1;
            cmd->Entities = (Entity*)((byte*)cmd + sizeof(CreateCommand));
            cmd->Entities[0] = newEntity;
        }

        internal void AddCreateCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity* newEntities, int count, EntityArchetype archetype)
        {
            var sizeNeeded = Align(sizeof(CreateCommand) + sizeof(Entity)*count, ALIGN_64_BIT);
            var cmd = (CreateCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.CommandType = op;
            cmd->Header.TotalSize = sizeNeeded;
            cmd->Header.SortKey = chain->m_LastSortKey;
            cmd->Archetype = archetype;
            cmd->EntityCount = count;
            cmd->Entities = (Entity*)((byte*)cmd + sizeof(CreateCommand));
            UnsafeUtility.MemCpy(cmd->Entities, newEntities, count * sizeof(Entity));
        }

        internal void AddEntityCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity newEntity, Entity e)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddEntityCommand");
            var sizeNeeded = Align(sizeof(EntityCommand)+sizeof(Entity), ALIGN_64_BIT);
            var cmd = (EntityCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.CommandType = op;
            cmd->Header.TotalSize = sizeNeeded;
            cmd->Header.SortKey = chain->m_LastSortKey;
            cmd->Entity = e;
            cmd->EntityCount = 1;
            cmd->Entities = (Entity*)((byte*)cmd + sizeof(EntityCommand));
            cmd->Entities[0] = newEntity;
        }

        internal void AddLinkedEntityGroupComponentCommand<T>(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, EntityQueryMask mask, Entity e, T component) where T : unmanaged, IComponentData
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddLinkedEntityGroupComponentCommand");
            var ctype = ComponentType.ReadWrite<T>();
            if (ctype.IsZeroSized)
            {
                AddLinkedEntityGroupTypeCommand(chain, sortKey, op, mask, e, ctype);
                return;
            }

            // NOTE: This has to be sizeof not TypeManager.SizeInChunk since we use UnsafeUtility.CopyStructureToPtr
            //       even on zero size components.
            short typeSize = (short)UnsafeUtility.SizeOf<T>();
            var sizeNeeded = Align(sizeof(EntityQueryMaskCommand) + typeSize, ALIGN_64_BIT);

            var cmd = (EntityQueryMaskCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.Header.CommandType = op;
            cmd->Header.Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Header.Entity = e;
            cmd->Header.Header.EntityCount = 0;
            cmd->Header.Header.Entities = null;
            cmd->Header.ComponentTypeIndex = ctype.TypeIndex;
            cmd->Header.ComponentSize = typeSize;
            cmd->Mask = mask;
            byte* componentValue = (byte*)(cmd + 1);
            UnsafeUtility.CopyStructureToPtr(ref component, componentValue);
        }

        internal void AddLinkedEntityGroupTypeCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op,
            EntityQueryMask mask, Entity e, ComponentType t)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddLinkedEntityGroupTypeCommand");
            var sizeNeeded = Align(sizeof(EntityQueryMaskCommand), ALIGN_64_BIT);

            var data = (EntityQueryMaskCommand*)Reserve(chain, sortKey, sizeNeeded);

            data->Header.Header.Header.CommandType = op;
            data->Header.Header.Header.TotalSize = sizeNeeded;
            data->Header.Header.Header.SortKey = chain->m_LastSortKey;
            data->Header.Header.Entity = e;
            data->Header.Header.EntityCount = 0;
            data->Header.Header.Entities = null;
            data->Header.ComponentTypeIndex = t.TypeIndex;
            data->Header.ComponentSize = 0;
            data->Mask = mask;
        }

        internal void AddMultipleEntityCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op, NativeArray<Entity> entities,
            Entity e)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddMultipleEntityCommand");
            int numEntities = entities.Length;
            var sizeNeeded = Align(sizeof(EntityCommand) + (sizeof(Entity) * numEntities), ALIGN_64_BIT);
            var cmd = (EntityCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.CommandType = op;
            cmd->Header.TotalSize = sizeNeeded;
            cmd->Header.SortKey = chain->m_LastSortKey;
            cmd->Entity = e;
            cmd->EntityCount = numEntities;
            cmd->Entities = (Entity*)((byte*)cmd + sizeof(EntityCommand));
            UnsafeUtility.MemCpy(cmd->Entities, entities.GetUnsafePtr(), sizeof(Entity) * numEntities);
        }

        internal void AddEntityComponentTypeWithValueCommand<T>(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, Entity e, T component) where T : unmanaged, IComponentData
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddEntityComponentCommand");
            var ctype = ComponentType.ReadWrite<T>();
            if (ctype.IsZeroSized)
            {
                AddEntityComponentTypeWithoutValueCommand(chain, sortKey, op, e, ctype);
                return;
            }

            // NOTE: This has to be sizeof not TypeManager.SizeInChunk since we use UnsafeUtility.CopyStructureToPtr
            //       even on zero size components.
            short typeSize = (short)UnsafeUtility.SizeOf<T>();
            var sizeNeeded = Align(sizeof(EntityComponentCommand) + typeSize, ALIGN_64_BIT);

            var cmd = (EntityComponentCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->ComponentTypeIndex = ctype.TypeIndex;
            cmd->ComponentSize = typeSize;
            byte* componentValue = (byte*)(cmd + 1);
            UnsafeUtility.CopyStructureToPtr(ref component, componentValue);
        }

        internal void UnsafeAddEntityComponentCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op,
            Entity e, TypeIndex typeIndex, int typeSize, void* componentDataPtr)
        {
            UnityEngine.Assertions.AssertCore.AreEqual(TypeManager.GetTypeInfo(typeIndex).TypeSize, typeSize,
                "Type size does not match TypeManager's size!");
            UnityEngine.Assertions.AssertCore.IsTrue(componentDataPtr != null, "componentDataPtr is null!");
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.UnsafeAddEntityComponentCommand");
            var sizeNeeded = Align(sizeof(EntityComponentCommand) + typeSize, ALIGN_64_BIT);

            var cmd = (EntityComponentCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->ComponentTypeIndex = typeIndex;
            cmd->ComponentSize = (short)typeSize;
            byte* componentValue = (byte*)(cmd + 1);
            UnsafeUtility.MemCpy(componentValue, componentDataPtr, typeSize);
        }

        internal void AddEntityEnabledCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity e,
            bool value)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AddEntityEnabledCommand");
            var sizeNeeded = Align(sizeof(EntityEnabledCommand), ALIGN_64_BIT);

            var cmd = (EntityEnabledCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->IsEnabled = value ? (byte)1 : (byte)0;
        }

        internal void AddEntityComponentEnabledCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op,
            Entity e, TypeIndex typeIndex, bool value)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddEntityComponentEnabledCommand");
            var sizeNeeded = Align(sizeof(EntityComponentEnabledCommand), ALIGN_64_BIT);

            var cmd = (EntityComponentEnabledCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.Header.CommandType = op;
            cmd->Header.Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Header.Entity = e;
            cmd->Header.Header.EntityCount = 0;
            cmd->Header.Header.Entities = null;
            cmd->ComponentTypeIndex = typeIndex;
            cmd->Header.IsEnabled = value ? (byte)1 : (byte)0;
        }

        internal void AddEntityNameCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op, Entity e,
            in FixedString64Bytes name)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddEntityComponentEnabledCommand");
            var sizeNeeded = Align(sizeof(EntityNameCommand), ALIGN_64_BIT);

            var cmd = (EntityNameCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->Name = name;
        }

        internal BufferHeader* AddEntityBufferCommand<T>(EntityCommandBufferChain* chain, int sortKey, ECBCommand op,
            Entity e, out int internalCapacity) where T : struct, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            ref readonly var type = ref TypeManager.GetTypeInfo<T>();

            // We use type.SizeInChunk here instead of sizeof(T), because what we're serializing in the command buffer
            // is a full BufferHeader + internal buffer of Ts, not just a single T. SizeInChunk for buffer types
            // implicitly takes this into account.
            var sizeNeeded = Align(sizeof(EntityBufferCommand) + type.SizeInChunk, ALIGN_64_BIT);

            var cmd = (EntityBufferCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->ComponentTypeIndex = typeIndex;
            cmd->ComponentSize = (short)type.SizeInChunk;

            BufferHeader* header = &cmd->BufferNode.TempBuffer;
            BufferHeader.Initialize(header, type.BufferCapacity);

            // Track all DynamicBuffer headers created during recording. Until the ECB is played back, it owns the
            // memory allocations for these buffers and is responsible for deallocating them when the ECB is disposed.
            cmd->BufferNode.Prev = chain->m_Cleanup->BufferCleanupList;
            chain->m_Cleanup->BufferCleanupList = &(cmd->BufferNode);
            // The caller may invoke methods on the DynamicBuffer returned by this command during ECB recording which
            // cause it to allocate memory (for example, DynamicBuffer.AddRange). These allocations always use
            // Allocator.Persistent, not the ECB's allocator. These allocations must ALWAYS be manually cleaned up
            // if the ECB is disposed without being played back. So, we have to force the full ECB cleanup process
            // to run in this case, even if it could normally be skipped.
            m_ForceFullDispose = true;

            internalCapacity = type.BufferCapacity;

            return header;
        }

        internal static int Align(int size, int alignmentPowerOfTwo)
        {
            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        internal void AddEntityComponentTypeWithoutValueCommand(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, Entity e, ComponentType t)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddEntityComponentTypeCommand");
            var sizeNeeded = Align(sizeof(EntityComponentCommand), ALIGN_64_BIT);

            var data = (EntityComponentCommand*)Reserve(chain, sortKey, sizeNeeded);

            data->Header.Header.CommandType = op;
            data->Header.Header.TotalSize = sizeNeeded;
            data->Header.Header.SortKey = chain->m_LastSortKey;
            data->Header.Entity = e;
            data->Header.EntityCount = 0;
            data->Header.Entities = null;
            data->ComponentTypeIndex = t.TypeIndex;
            data->ComponentSize = 0;
        }

        internal void AddEntityComponentTypesCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op,
            Entity e, in ComponentTypeSet t)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddEntityComponentTypesCommand");
            var sizeNeeded = Align(sizeof(EntityMultipleComponentsCommand), ALIGN_64_BIT);

            var data = (EntityMultipleComponentsCommand*)Reserve(chain, sortKey, sizeNeeded);

            data->Header.Header.CommandType = op;
            data->Header.Header.TotalSize = sizeNeeded;
            data->Header.Header.SortKey = chain->m_LastSortKey;
            data->Header.Entity = e;
            data->Header.EntityCount = 0;
            data->Header.Entities = null;
            data->TypeSet = t;
        }

        internal bool AppendMultipleEntitiesCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op,
            EntityQuery entityQuery)
        {
            var entities = entityQuery.ToEntityArray(m_Allocator.ToAllocator);
            if (entities.Length == 0)
            {
                entities.Dispose();
                return false;
            }

            var result = AppendMultipleEntitiesCommand(chain, sortKey, op,
                (Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length);
            AtomicSafetyHandle.DisposeHandle(ref entities
                .m_Safety); // dispose safety handle, but we'll dispose of the actual data in playback
            return result;
        }

        internal bool AppendMultipleEntitiesCommand(EntityCommandBufferChain* chain, int sortKey, ECBCommand op,
            Entity* entities, int entityCount)
        {
            var sizeNeeded = Align(sizeof(MultipleEntitiesCommand), ALIGN_64_BIT);


            var cmd = (MultipleEntitiesCommand*)Reserve(chain, sortKey, sizeNeeded);
            cmd->Entities.Ptr = entities;
            cmd->Entities.Prev = chain->m_Cleanup->EntityArraysCleanupList;
            chain->m_Cleanup->EntityArraysCleanupList = &(cmd->Entities);

            cmd->EntitiesCount = entityCount;
            cmd->Allocator = m_Allocator;

            cmd->Header.CommandType = op;
            cmd->Header.TotalSize = sizeNeeded;
            cmd->Header.SortKey = chain->m_LastSortKey;

            return true;
        }

        internal bool AppendMultipleEntitiesComponentCommandWithValue<T>(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, EntityQuery entityQuery, T component) where T : unmanaged, IComponentData
        {
            var entities = entityQuery.ToEntityArray(m_Allocator.ToAllocator); // disposed in playback
            if (entities.Length == 0)
            {
                entities.Dispose();
                return false;
            }

            var result = AppendMultipleEntitiesComponentCommandWithValue(chain, sortKey, op,
                (Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length, component);
            AtomicSafetyHandle.DisposeHandle(ref entities
                .m_Safety); // dispose safety handle, but we'll dispose of the actual data in playback
            return result;
        }

        internal bool AppendMultipleEntitiesComponentCommandWithValue<T>(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, Entity* entities, int entityCount, T component)
            where T : unmanaged, IComponentData
        {
            var ctype = ComponentType.ReadWrite<T>();
            if (ctype.IsZeroSized)
                return AppendMultipleEntitiesComponentCommand(chain, sortKey, op, entities, entityCount, ctype);

            var typeSize = (short)UnsafeUtility.SizeOf<T>();
            var sizeNeeded = Align(sizeof(MultipleEntitiesComponentCommand) + typeSize, ALIGN_64_BIT);


            var cmd = (MultipleEntitiesComponentCommand*)Reserve(chain, sortKey, sizeNeeded);
            cmd->Header.Entities.Ptr = entities;
            cmd->Header.Entities.Prev = chain->m_Cleanup->EntityArraysCleanupList;
            chain->m_Cleanup->EntityArraysCleanupList = &(cmd->Header.Entities);

            cmd->Header.EntitiesCount = entityCount;
            cmd->Header.Allocator = m_Allocator;

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->ComponentTypeIndex = ctype.TypeIndex;
            cmd->ComponentSize = typeSize;
            byte* componentValue = (byte*)(cmd + 1);
            UnsafeUtility.CopyStructureToPtr(ref component, componentValue);
            return true;
        }

        internal bool AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
            EntityCommandBufferChain* chain,
            int sortKey,
            ECBCommand op,
            EntityQuery entityQuery,
            int hashCode,
            void* componentAddr)
            where T : struct, ISharedComponentData
        {
            var entities = entityQuery.ToEntityArray(m_Allocator.ToAllocator);
            if (entities.Length == 0)
            {
                entities.Dispose();
                return false;
            }

            AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
                chain,
                sortKey,
                op,
                (Entity*)entities.GetUnsafeReadOnlyPtr(),
                entities.Length,
                hashCode,
                componentAddr);

            AtomicSafetyHandle.DisposeHandle(ref entities
                .m_Safety); // dispose safety handle, but we'll dispose of the actual data in playback
            return true;
        }

        internal bool AppendEntityQueryComponentCommandWithUnmanagedSharedValue<T>(
            EntityCommandBufferChain* chain,
            int sortKey,
            ECBCommand op,
            EntityQuery entityQuery,
            int hashCode,
            void* componentAddr)
            where T : struct, ISharedComponentData
        {
            var ctype = ComponentType.ReadWrite<T>();
            var typeSize = UnsafeUtility.SizeOf<T>();
            var sizeNeeded = Align(sizeof(EntityQueryComponentCommandWithUnmanagedSharedComponent) + typeSize,
                ALIGN_64_BIT);


            var cmd = (EntityQueryComponentCommandWithUnmanagedSharedComponent*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.Header.CommandType = op;
            cmd->Header.Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Header.QueryImpl = entityQuery._GetImpl();
            cmd->Header.ComponentTypeIndex = ctype.TypeIndex;
            cmd->ComponentSize = typeSize;
            cmd->HashCode = hashCode;
            cmd->IsDefault = componentAddr == null ? (byte)1 : (byte)0;
            byte* componentValue = (byte*)(cmd + 1);
            if (componentAddr != null)
            {
                UnsafeUtility.MemCpy(componentValue, componentAddr, typeSize);
            }
            else
            {
                UnsafeUtility.MemSet(componentValue, 0, typeSize);
            }

            return true;
        }

        internal bool AppendMultipleEntitiesCommand_WithUnmanagedSharedValue<T>(
            EntityCommandBufferChain* chain,
            int sortKey,
            ECBCommand op,
            Entity* entities,
            int entityCount,
            int hashCode,
            void* componentAddr) where T : struct, ISharedComponentData
        {
            var ctype = ComponentType.ReadWrite<T>();
            var typeSize = UnsafeUtility.SizeOf<T>();
            var sizeNeeded = Align(sizeof(MultipleEntitiesCommand_WithUnmanagedSharedComponent) + typeSize,
                ALIGN_64_BIT);


            var cmd = (MultipleEntitiesCommand_WithUnmanagedSharedComponent*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Entities.Ptr = entities;
            cmd->Header.Entities.Prev = chain->m_Cleanup->EntityArraysCleanupList;
            chain->m_Cleanup->EntityArraysCleanupList = &(cmd->Header.Entities);

            cmd->Header.EntitiesCount = entityCount;
            cmd->Header.Allocator = m_Allocator;

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->ComponentTypeIndex = ctype.TypeIndex;
            cmd->ComponentSize = typeSize;
            cmd->HashCode = hashCode;
            cmd->IsDefault = componentAddr == null ? (byte)1 : (byte)0;
            byte* componentValue = (byte*)(cmd + 1);
            if (componentAddr != null)
            {
                UnsafeUtility.MemCpy(componentValue, componentAddr, typeSize);
            }
            else
            {
                UnsafeUtility.MemSet(componentValue, 0, typeSize);
            }

            return true;
        }

        internal bool AppendEntityQueryCommand(
            EntityCommandBufferChain* chain,
            int sortKey,
            ECBCommand op,
            EntityQuery entityQuery)
        {
            var sizeNeeded = Align(sizeof(EntityQueryCommand), ALIGN_64_BIT);


            var cmd = (EntityQueryCommand*)Reserve(chain, sortKey, sizeNeeded);
            cmd->QueryImpl = entityQuery._GetImpl();

            cmd->Header.CommandType = op;
            cmd->Header.TotalSize = sizeNeeded;
            cmd->Header.SortKey = chain->m_LastSortKey;
            return true;
        }

        internal bool AppendEntityQueryComponentCommand(
            EntityCommandBufferChain* chain,
            int sortKey,
            ECBCommand op,
            EntityQuery entityQuery,
            ComponentType t)
        {
            var sizeNeeded = Align(sizeof(EntityQueryComponentCommand), ALIGN_64_BIT);


            var cmd = (EntityQueryComponentCommand*)Reserve(chain, sortKey, sizeNeeded);
            cmd->Header.QueryImpl = entityQuery._GetImpl();

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->ComponentTypeIndex = t.TypeIndex;
            return true;
        }

        internal bool AppendEntityQueryComponentTypeSetCommand(
            EntityCommandBufferChain* chain,
            int sortKey,
            ECBCommand op,
            EntityQuery entityQuery,
            in ComponentTypeSet typeSet)
        {
            var sizeNeeded = Align(sizeof(EntityQueryComponentTypeSetCommand), ALIGN_64_BIT);


            var cmd = (EntityQueryComponentTypeSetCommand*)Reserve(chain, sortKey, sizeNeeded);
            cmd->Header.QueryImpl = entityQuery._GetImpl();

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->TypeSet = typeSet;
            return true;
        }

        internal bool AppendMultipleEntitiesComponentCommand(
            EntityCommandBufferChain* chain,
            int sortKey,
            ECBCommand op,
            EntityQuery entityQuery,
            ComponentType t)
        {
            var entities = entityQuery.ToEntityArray(m_Allocator.ToAllocator); // disposed in playback
            if (entities.Length == 0)
            {
                entities.Dispose();
                return false;
            }

            var result = AppendMultipleEntitiesComponentCommand(chain, sortKey, op,
                (Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length, t);
            AtomicSafetyHandle.DisposeHandle(ref entities
                .m_Safety); // dispose safety handle, but we'll dispose of the actual data in playback
            return result;
        }

        internal bool AppendMultipleEntitiesComponentCommand(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op,
            Entity* entities, int entityCount, ComponentType t)
        {
            var sizeNeeded = Align(sizeof(MultipleEntitiesComponentCommand), ALIGN_64_BIT);


            var cmd = (MultipleEntitiesComponentCommand*)Reserve(chain, sortKey, sizeNeeded);
            cmd->Header.Entities.Ptr = entities;
            cmd->Header.Entities.Prev = chain->m_Cleanup->EntityArraysCleanupList;
            chain->m_Cleanup->EntityArraysCleanupList = &(cmd->Header.Entities);

            cmd->Header.EntitiesCount = entityCount;
            cmd->Header.Allocator = m_Allocator;

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->ComponentTypeIndex = t.TypeIndex;
            cmd->ComponentSize = 0; // signifies that the command doesn't include a value for the new component
            return true;
        }

        internal bool AppendMultipleEntitiesMultipleComponentsCommand(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, EntityQuery entityQuery, in ComponentTypeSet t)
        {
            var entities = entityQuery.ToEntityArray(m_Allocator.ToAllocator); // disposed in playback
            if (entities.Length == 0)
            {
                entities.Dispose();
                return false;
            }

            var result = AppendMultipleEntitiesMultipleComponentsCommand(chain, sortKey, op,
                (Entity*)entities.GetUnsafeReadOnlyPtr(), entities.Length, t);
            AtomicSafetyHandle.DisposeHandle(ref entities
                .m_Safety); // dispose safety handle, but we'll dispose of the actual data in playback
            return result;
        }

        internal bool AppendMultipleEntitiesMultipleComponentsCommand(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, Entity* entities, int entityCount, in ComponentTypeSet t)
        {
            var sizeNeeded = Align(sizeof(MultipleEntitiesAndComponentsCommand), ALIGN_64_BIT);


            var cmd = (MultipleEntitiesAndComponentsCommand*)Reserve(chain, sortKey, sizeNeeded);
            cmd->Header.Entities.Ptr = entities;
            cmd->Header.Entities.Prev = chain->m_Cleanup->EntityArraysCleanupList;
            chain->m_Cleanup->EntityArraysCleanupList = &(cmd->Header.Entities);

            cmd->Header.EntitiesCount = entityCount;
            cmd->Header.Allocator = m_Allocator;

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;

            cmd->TypeSet = t;
            return true;
        }

        internal void AddEntityUnmanagedSharedComponentCommand<T>(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, Entity e, int hashCode, void* componentData)
            where T : struct
        {
            // NOTE: This has to be sizeof not TypeManager.SizeInChunk since we use UnsafeUtility.CopyStructureToPtr
            //       even on zero size components.
            var typeSize = UnsafeUtility.SizeOf<T>();
            var typeIndex = TypeManager.GetTypeIndex<T>();
            AddEntityUnmanagedSharedComponentCommand(chain, sortKey, op, e, hashCode, typeIndex, typeSize,
                componentData);
        }

        internal void AddEntityUnmanagedSharedComponentCommand(EntityCommandBufferChain* chain, int sortKey,
            ECBCommand op, Entity e, int hashCode, TypeIndex typeIndex, int typeSize, void* componentData)
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException(
                    "Invalid Entity.Null passed. ECBCommand.AddEntityUnmanagedSharedComponentCommand");
            var sizeNeeded = Align(sizeof(EntityUnmanagedSharedComponentCommand) + typeSize, ALIGN_64_BIT);

            var cmd = (EntityUnmanagedSharedComponentCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = op;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->ComponentTypeIndex = typeIndex;
            cmd->HashCode = hashCode;
            cmd->IsDefault = componentData == null ? (byte)1 : (byte)0;
            byte* componentValue = (byte*)(cmd + 1);
            if (componentData != null)
            {
                UnsafeUtility.MemCpy(componentValue, componentData, typeSize);
            }
            else
            {
                UnsafeUtility.MemSet(componentValue, 0, typeSize);
            }
        }

        internal byte* Reserve(EntityCommandBufferChain* chain, int sortKey, int size)
        {
            var align = Align(size, ALIGN_64_BIT);
            if (align != size)
                Assert.IsTrue(false,
                    $"Misaligned size. Expected alignment of {ALIGN_64_BIT} but was {align}. Unaligned access can cause crashes on platforms such as ARM.");
            if (Hint.Unlikely(chain->m_Head == null))
                EntityCommandBufferChain.InitChain(chain, m_Allocator);

            int newSortKey = sortKey;
            if (Hint.Unlikely(newSortKey < chain->m_LastSortKey))
            {
                // copy current chain to new next and reset current chain
                EntityCommandBufferChain* nextChain =
                    (EntityCommandBufferChain*)Memory.Unmanaged.Allocate(sizeof(EntityCommandBufferChain), ALIGN_64_BIT,
                        m_Allocator);
                *nextChain = *chain;
                EntityCommandBufferChain.InitChain(chain, m_Allocator);
                chain->m_NextChain = nextChain;
            }

            chain->m_LastSortKey = newSortKey;

            if (Hint.Unlikely(chain->m_Tail == null || chain->m_Tail->Capacity < size))
            {
                var chunkSize = math.max(m_MinimumChunkSize, size);

                var c = (ECBChunk*)Memory.Unmanaged.Allocate(sizeof(ECBChunk) + chunkSize, 16, m_Allocator);
                var prev = chain->m_Tail;
                c->Next = null;
                c->Prev = prev;
                c->Used = 0;
                c->Size = chunkSize;

                if (prev != null) prev->Next = c;

                if (chain->m_Head == null)
                {
                    chain->m_Head = c;
                    // This seems to be the best place to track the number of non-empty command buffer chunks
                    // during the recording process.
                    Interlocked.Increment(ref m_RecordedChainCount);
                }

                chain->m_Tail = c;
            }

            var offset = chain->m_Tail->Bump(size);
            var ptr = (byte*)chain->m_Tail + sizeof(ECBChunk) + offset;
            return ptr;
        }

        public DynamicBuffer<T> CreateBufferCommand<T>(ECBCommand commandType, EntityCommandBufferChain* chain,
            int sortKey, Entity e, AtomicSafetyHandle bufferSafety, AtomicSafetyHandle arrayInvalidationSafety)
            where T : unmanaged, IBufferElementData
        {
            int internalCapacity;
            BufferHeader* header = AddEntityBufferCommand<T>(chain, sortKey, commandType, e, out internalCapacity);

            var safety = bufferSafety;
            AtomicSafetyHandle.UseSecondaryVersion(ref safety);
            var arraySafety = arrayInvalidationSafety;
            return new DynamicBuffer<T>(header, safety, arraySafety, false, false, 0, internalCapacity);
        }

        public void AppendToBufferCommand<T>(EntityCommandBufferChain* chain, int sortKey, Entity e, T element)
            where T : struct, IBufferElementData
        {
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AppendToBufferCommand");
            var typeIndex = TypeManager.GetTypeIndex<T>();
            // NOTE: This has to be sizeof not TypeManager.SizeInChunk since we use UnsafeUtility.CopyStructureToPtr
            //       even on zero size components.
            var typeSize = (short)UnsafeUtility.SizeOf<T>();
            var sizeNeeded = Align(sizeof(EntityComponentCommand) + typeSize, ALIGN_64_BIT);

            var cmd = (EntityComponentCommand*)Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = ECBCommand.AppendToBuffer;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.EntityCount = 0;
            cmd->Header.Entities = null;
            cmd->ComponentTypeIndex = typeIndex;
            cmd->ComponentSize = typeSize;
            byte* componentValue = (byte*)(cmd + 1);
            UnsafeUtility.CopyStructureToPtr(ref element, componentValue);
        }
    }
}
