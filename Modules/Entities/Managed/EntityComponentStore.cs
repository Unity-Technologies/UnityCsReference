// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Threading;
using Unity.Scripting.LifecycleManagement;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Assertions;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Entities.LowLevel;
using UnityEngine;
using Assert = UnityEngine.Assertions.AssertCore;

// ---------------------------------------------------------------------------------------------------------
// EntityComponentStore
// ---------------------------------------------------------------------------------------------------------
// - Internal interface to Archetype, Entity, and Chunk data.
// - Throwing exceptions in this code not supported. (Can be called from burst delegate from main thread.)
// ---------------------------------------------------------------------------------------------------------

// Notes on upcoming changes to EntityComponentStore:
//
// Checklist @macton Where is entityComponentStore and the EntityBatch interface going?
// [ ] Replace all internal interfaces to entityComponentStore to work with EntityBatch via entityComponentStore
//   [x] Convert AddComponent NativeArray<Entity>
//   [x] Convert AddComponent NativeArray<ArchetypeChunk>
//   [x] Convert AddSharedComponent NativeArray<ArchetypeChunk>
//   [x] Convert AddChunkComponent NativeArray<ArchetypeChunk>
//   [x] Move AddComponents(entity)
//   [ ] Need AddComponents for NativeList<EntityBatch>
//   [ ] Convert DestroyEntities
//   [x] Convert RemoveComponent NativeArray<ArchetypeChunk>
//   [x] Convert RemoveComponent Entity
// [x] EntityDataManager just becomes thin shim on top of EntityComponentStore
// [x] Remove EntityDataManager
// [x] Rework internal storage so that structural changes are blittable (and burst job)
// [ ] Expose EntityBatch interface public via EntityManager
// [ ] Other structural interfaces (e.g. NativeArray<Entity>) are then (optional) utility functions.
//
// 1. Ideally EntityComponentStore is the internal interface that EntityCommandBuffer can use (fast).
// 2. That would be the only access point for SystemBase.
// 3. "Easy Mode" can have (the equivalent) of EntityManager as utility functions on EntityComponentStore.
// 4. EntityDataManager goes away.
//
// Input data protocol to support for structural changes:
//    1. NativeList<EntityBatch>
//    2. NativeArray<ArchetypeChunk>
//    3. Entity
//
// Expected public (internal) API:
//
// ** Add Component **
//
// IComponentData and ISharedComponentData can be added via:
//    AddComponent NativeList<EntityBatch>
//    AddComponent Entity
//    AddComponents NativeList<EntityBatch>
//    AddComponents Entity
//
// Chunk Components can only be added via;
//    AddChunkComponent NativeArray<ArchetypeChunk>
//
// Alternative to add ISharedComponeentData when changing whole chunks.
//    AddSharedComponent NativeArray<ArchetypeChunk>
//
// ** Remove Component **
//
// Any component type can be removed via:
//    RemoveComponent NativeList<EntityBatch>
//    RemoveComponent Entity
//    RemoveComponent NativeArray<ArchetypeChunk>
//    RemoveComponents NativeList<EntityBatch>
//    RemoveComponents Entity
//    RemoveComponents NativeArray<ArchetypeChunk>


namespace Unity.Entities
{
    internal unsafe struct CompanionDeferredCommands : IDisposable
    {
        public UnsafeAppendBuffer CommandBuffer;
        public bool Empty => CommandBuffer.IsEmpty;

        public enum Command
        {
            RepointCompanionComponents,
            DestroyCompanionGameObjects,
        }

        public void Init()
        {
            CommandBuffer = new UnsafeAppendBuffer(1024, 16, Allocator.Persistent);
        }

        public void Dispose()
        {
            CommandBuffer.Dispose();
        }

        public void Reset()
        {
            CommandBuffer.Reset();
        }

        // Queues a repoint of the cloned entities' CompanionComponent<T> wrappers at each clone's own companion
        // component. Deferred to Playback because resolving the component from its EntityId is a managed main-thread call.
        public void QueueRepointCompanionComponents(Entity* dstEntities, EntityId* dstCompanionLinkIds, int instanceCount)
        {
            CommandBuffer.Add<int>((int)Command.RepointCompanionComponents);
            CommandBuffer.AddArray<Entity>(dstEntities, instanceCount);
            CommandBuffer.AddArray<EntityId>(dstCompanionLinkIds, instanceCount);
        }

        public void QueueDestroyCompanionGameObjects(EntityId* companionLinkIds, int count)
        {
            CommandBuffer.Add<int>((int)Command.DestroyCompanionGameObjects);
            CommandBuffer.AddArray<EntityId>(companionLinkIds, count);
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ComponentTypeList
    {
        internal void* Ptr;
        internal int Length;
        internal int Capacity;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal ComponentTypeList(int sizeOf, int alignOf, int initialCapacity, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory) : this()
        {
            Allocator = allocator;
            Ptr = null;
            Length = 0;
            Capacity = 0;

            if (initialCapacity != 0)
            {
                SetCapacity(ref Allocator, sizeOf, alignOf, initialCapacity);
            }

            if (options == NativeArrayOptions.ClearMemory
                && Ptr != null)
            {
                UnsafeUtility.MemClear(Ptr, Capacity * sizeOf);
            }
        }

        internal bool IsEmpty => !IsCreated || Length == 0;

        internal bool IsCreated => Ptr != null;

        internal void Dispose()
        {
            if (CollectionHelper.ShouldDeallocate(Allocator))
            {
                AllocatorManager.Free(Allocator, Ptr);
                Allocator = AllocatorManager.Invalid;
            }

            Ptr = null;
            Length = 0;
            Capacity = 0;
        }

        internal void Clear()
        {
            Length = 0;
        }

        internal void Resize(int sizeOf, int alignOf, int length, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            var oldLength = Length;

            if (length > Capacity)
            {
                SetCapacity(ref Allocator, sizeOf, alignOf, length);
            }

            Length = length;

            if (options == NativeArrayOptions.ClearMemory
                && oldLength < length)
            {
                var num = length - oldLength;
                byte* ptr = (byte*)Ptr;
                UnsafeUtility.MemClear(ptr + oldLength * sizeOf, num * sizeOf);
            }
        }

        void Realloc<U>(ref U allocator, int sizeOf, int alignOf, int capacity) where U : unmanaged, AllocatorManager.IAllocator
        {
            void* newPointer = null;

            if (capacity > 0)
            {
                newPointer = allocator.Allocate(sizeOf, alignOf, capacity);

                if (Capacity > 0)
                {
                    var itemsToCopy = math.min(capacity, Capacity);
                    var bytesToCopy = itemsToCopy * sizeOf;
                    UnsafeUtility.MemCpy(newPointer, Ptr, bytesToCopy);
                }
            }

            allocator.Free(Ptr, sizeOf, alignOf, Capacity);

            Ptr = newPointer;
            Capacity = capacity;
            Length = math.min(Length, capacity);
        }

        void SetCapacity<U>(ref U allocator, int sizeOf, int alignOf, int capacity) where U : unmanaged, AllocatorManager.IAllocator
        {
            var newCapacity = math.max(capacity, 64 / sizeOf);
            newCapacity = math.ceilpow2(newCapacity);

            if (newCapacity == Capacity)
            {
                return;
            }

            Realloc(ref allocator, sizeOf, alignOf, newCapacity);
        }
    }

    internal unsafe partial struct EntityComponentStore
    {
        private const int kUnmanagedSharedComponentIndexFlag = 1 << 31;

        internal const int kDefaultAllocatorBudgetInBytes = 16 * 1024 * 1024;
        [NoAutoStaticsCleanup] // DefaultWorldInitialization sets it before it creates worlds.
        internal static int ArchetypeChunkAllocatorBudgetInBytes = kDefaultAllocatorBudgetInBytes;

        [NativeDisableUnsafePtrRestriction]
        int* m_ComponentTypeOrderVersion;

        BlockAllocator m_ArchetypeChunkAllocator;

        internal int ArchetypeChunkAllocatorUsedBytes => m_ArchetypeChunkAllocator.AllocatedBytes;
        internal int ArchetypeChunkAllocatorBudgetBytes => m_ArchetypeChunkAllocator.BudgetInBytes;

        internal UnsafePtrList<Archetype> m_Archetypes;

        ArchetypeListMap m_TypeLookup;

        internal CompanionDeferredCommands CompanionChangesTracker;

        internal int m_SharedComponentVersion;
        internal int m_SharedComponentGlobalVersion;

        private int m_UnmanagedSharedComponentCount;
        internal UnsafeHashMap<int, ComponentTypeList> m_UnmanagedSharedComponentsByType;

        struct SharedComponentInfo
        {
            public int RefCount;
            public int ComponentType;
            public int Version;
            public int HashCode;
        }

        private UnsafeList<TypeIndex> m_UnmanagedSharedComponentTypes;
        private UnsafeHashMap<int, UnsafeList<SharedComponentInfo>> m_UnmanagedSharedComponentInfo;
        private UnsafeParallelMultiHashMap<ulong, int> m_HashLookup;

        internal ChunkListChanges m_ChunkListChangesTracker;

        ulong m_WorldSequenceNumber;
        ulong m_NextChunkSequenceNumber;


        uint m_GlobalSystemVersion;
        int  m_IntentionallyInconsistent;
        uint m_ArchetypeTrackingVersion;

        TypeIndex m_LinkedGroupType;
        TypeIndex m_ChunkHeaderType;
        TypeIndex m_PrefabType;
        TypeIndex m_OmitLinkedEntityGroupFromPrefabInstanceType;
        TypeIndex m_CleanupEntityType;
        TypeIndex m_DisabledType;
        TypeIndex m_EntityType;
        TypeIndex m_SystemInstanceType;

        ComponentType m_ChunkHeaderComponentType;
        ComponentType m_EntityComponentType;
        ComponentType m_SimulateComponentType;

        HashMapHelper<int>* m_TypeInfos;
        TypeManager.EntityOffsetInfo* m_EntityOffsetInfos;

        internal int m_DebugOnlyManagedAccess;

        internal byte memoryInitPattern;
        internal byte useMemoryInitPattern;        // should be bool, but it doesn't get along nice with burst so far, so we use a byte instead

        StructuralChangesProfiler.Recorder* m_StructuralChangesRecorder;
        internal ref StructuralChangesProfiler.Recorder StructuralChangesRecorder => ref (*m_StructuralChangesRecorder);

        const int kMaximumEmptyChunksInPool = 16; // can't alloc forever
        const int kDefaultCapacity = 1024;
        internal const int kMaxSharedComponentCount = 16;

        // Starting capacity of the per-type shared component maps. Exceeding it only costs a rehash,
        // so this is sized for a typical world rather than for the worst case.
        const int kInitialSharedComponentTypeCapacity = 128;

        public ulong WorldSequenceNumber => m_WorldSequenceNumber;

        struct AddressSpaceTagType { }
        static readonly SharedStatic<ulong> s_TotalChunkAddressSpaceInBytes = SharedStatic<ulong>.GetOrCreate<AddressSpaceTagType>();

        static readonly ulong DefaultChunkAddressSpaceInBytes = 1024UL * 1024UL * 1024UL;

        public static ulong TotalChunkAddressSpaceInBytes
        {
            get => s_TotalChunkAddressSpaceInBytes.Data > 0 ? s_TotalChunkAddressSpaceInBytes.Data - 1 : DefaultChunkAddressSpaceInBytes;
            set => s_TotalChunkAddressSpaceInBytes.Data = value + 1;
        }


        public EntityNameStoreAccess m_NameStoreAccess;
        public EntityNameStoreAccess NameStoreAccess => m_NameStoreAccess;
        public ulong NameChangeBitsSequenceNum => m_NameStoreAccess.NameChangeBitsSequenceNum;

        public ulong IncNameChangeBitsVersion()
        {
            return m_NameStoreAccess.IncNameChangeBitsVersion();
        }

        public void SetNameChangeBitsVersion(ulong nameChangeBitsVersion)
        {
            m_NameStoreAccess.SetNameChangeBitsVersion(nameChangeBitsVersion);
        }

        public EntityName GetEntityName(Entity entity)
        {
            return EntityNameStorage.GetEntityName(entity);
        }

        public void CopyAndUpdateNameByEntity(EntityComponentStore *fromEntityComponentStore, NativeArray<ArchetypeChunk> srcChunks, NativeArray<EntityRemapUtility.EntityRemapInfo> remap)
        {
            // TODO: Copy the names, maybe in a parallel job?
            for (int ci = 0, cc = srcChunks.Length; ci < cc; ci++)
            {
                var srcChunk = srcChunks[ci].m_Chunk;

                var srcEntities = (Entity*)srcChunk.Buffer;
                var ec = srcChunk.Count;
                for (int ei = 0; ei < ec; ei++)
                {
                    var srcEntity = srcEntities[ei];
                    var dstEntity = EntityRemapUtility.RemapEntity(ref remap, srcEntity);

                    var entityName = EntityNameStorage.GetEntityName(srcEntity);
                    EntityNameStorage.SetEntityName(dstEntity, entityName);
                    if (entityName.Index != 0)
                        m_NameStoreAccess.AddEntityToEntitiesWithNames(dstEntity);
                }
            }

            // Increase the version on both stores
            var srcNameChangeBitsSequenceNum = IncNameChangeBitsVersion();
            var targetNameChangeBitsSequenceNum = fromEntityComponentStore->IncNameChangeBitsVersion();

            // Now the names of the entities in 2 worlds are the same.
            // Set name change sequence number to the latest one and clear name change bitmap
            var newerNameChangeBitsSequenceNum = math.max(srcNameChangeBitsSequenceNum, targetNameChangeBitsSequenceNum);
            SetNameChangeBitsVersion(newerNameChangeBitsSequenceNum);
            fromEntityComponentStore->SetNameChangeBitsVersion(newerNameChangeBitsSequenceNum);
        }


        public int EntityOrderVersion => GetComponentTypeOrderVersion(m_EntityType);
        public uint GlobalSystemVersion => m_GlobalSystemVersion;

        public void IncrementGlobalSystemVersion(in SystemHandle handle = default)
        {
            ChangeVersionUtility.IncrementGlobalSystemVersion(ref m_GlobalSystemVersion);
        }

        public void SetGlobalSystemVersion(uint value, in SystemHandle handle = default)
        {
            m_GlobalSystemVersion = value;
        }


        internal bool IsIntentionallyInconsistent => m_IntentionallyInconsistent == 1;
        internal const long k_MaximumEntitiesPerWorld = 128L * 1024L * 1024L; // roughly 128 million Entities per World, maximum
        //This constant depends on the TypeManager.MaximumTypesCount (1 << 12) bits. If that changes, this should accordinlgy.
        internal const int kUnmanagedSharedTypeIndexBitOffset = 32 - 12 - 1; //19 bits
        internal const int kUnmanagedSharedElementIndexMask = (1 << kUnmanagedSharedTypeIndexBitOffset) - 1;

        void ResizeUnmanagedArrays(long oldValue, long newValue)
        {
        }


        public Entity GetEntityByEntityIndex(int index)
        {
            return s_entityStore.Data.GetEntityByEntityIndex(index);
        }


        public static void Create(EntityComponentStore* entities, ulong worldSequenceNumber, int newCapacity = kDefaultCapacity)
        {
            PerChunkArray.Initialize();
            ChunkStore.Initialize();

            UnsafeUtility.MemClear(entities, sizeof(EntityComponentStore));

            entities->m_NameStoreAccess = new EntityNameStoreAccess(entities);
            entities->m_GlobalSystemVersion = ChangeVersionUtility.InitialGlobalSystemVersion;

            entities->m_ComponentTypeOrderVersion = Memory.Unmanaged.Array.Allocate<int>(TypeManager.MaximumTypesCount, Allocator.Persistent);
            Memory.Array.Clear(entities->m_ComponentTypeOrderVersion, TypeManager.MaximumTypesCount);

            entities->m_ArchetypeChunkAllocator = new BlockAllocator(AllocatorManager.Persistent, ArchetypeChunkAllocatorBudgetInBytes);
            entities->m_TypeLookup = new ArchetypeListMap();
            entities->m_TypeLookup.Init(16);
            entities->m_WorldSequenceNumber = worldSequenceNumber;
            entities->m_NextChunkSequenceNumber = worldSequenceNumber << 32;
            entities->m_Archetypes = new UnsafePtrList<Archetype>(0, Allocator.Persistent);
            entities->CompanionChangesTracker = new CompanionDeferredCommands();
            entities->CompanionChangesTracker.Init();
            entities->m_SharedComponentVersion = 0;
            entities->m_SharedComponentGlobalVersion = 1;
            entities->m_UnmanagedSharedComponentCount = 0;
            entities->m_UnmanagedSharedComponentTypes = new UnsafeList<TypeIndex>(64, Allocator.Persistent);
            // Keyed by TypeIndex.Index, and only ever populated for shared component types, of which a
            // world uses a small fraction of the type index space. Sizing these by MaximumTypesCount
            // spent 192kB per world to hold a few dozen live entries.
            entities->m_UnmanagedSharedComponentsByType = new UnsafeHashMap<int, ComponentTypeList>(kInitialSharedComponentTypeCapacity, Allocator.Persistent);
            entities->m_UnmanagedSharedComponentInfo = new UnsafeHashMap<int, UnsafeList<SharedComponentInfo>>(kInitialSharedComponentTypeCapacity, Allocator.Persistent);
            entities->m_HashLookup = new UnsafeParallelMultiHashMap<ulong, int>(128, Allocator.Persistent);
            entities->m_LinkedGroupType = TypeManager.GetTypeIndex<LinkedEntityGroup>();
            entities->m_ChunkHeaderType = TypeManager.GetTypeIndex<ChunkHeader>();
            entities->m_PrefabType = TypeManager.GetTypeIndex<Prefab>();
            entities->m_OmitLinkedEntityGroupFromPrefabInstanceType = TypeManager.GetTypeIndex<OmitLinkedEntityGroupFromPrefabInstance>();
            entities->m_CleanupEntityType = TypeManager.GetTypeIndex<CleanupEntity>();
            entities->m_DisabledType = TypeManager.GetTypeIndex<Disabled>();
            entities->m_EntityType = TypeManager.GetTypeIndex<Entity>();
            entities->m_SystemInstanceType = TypeManager.GetTypeIndex<SystemInstance>();

            entities->m_ChunkHeaderComponentType = ComponentType.ReadWrite<ChunkHeader>();
            entities->m_EntityComponentType = ComponentType.ReadWrite<Entity>();
            entities->m_SimulateComponentType = ComponentType.ReadWrite<Simulate>();
            entities->InitializeTypeManagerPointers();

            entities->m_ChunkListChangesTracker = new ChunkListChanges();
            entities->m_ChunkListChangesTracker.Init();

            entities->m_StructuralChangesRecorder = Memory.Unmanaged.Allocate<StructuralChangesProfiler.Recorder>(Allocator.Persistent);
            entities->m_StructuralChangesRecorder->Initialize(Allocator.Persistent);

            // Sanity check a few alignments
            // Buffer should be 16 byte aligned to ensure component data layout itself can guarantee being aligned
            var offset = UnsafeUtility.GetFieldOffset(typeof(Chunk).GetField("Buffer"));
            Assert.IsTrue(offset % TypeManager.MaximumSupportedAlignment == 0, $"Chunk buffer must be {TypeManager.MaximumSupportedAlignment} byte aligned (buffer offset at {offset})");
            Assert.IsTrue(sizeof(Entity) == 8, $"Unity.Entities.Entity is expected to be 8 bytes in size (is {sizeof(Entity)}); if this changes, update Chunk explicit layout");
            var bufHeaderSize = UnsafeUtility.SizeOf<BufferHeader>();
            Assert.IsTrue(bufHeaderSize % TypeManager.MaximumSupportedAlignment == 0,
                $"BufferHeader total struct size must be a multiple of the max supported alignment ({TypeManager.MaximumSupportedAlignment})");
        }

        internal void InitializeTypeManagerPointers()
        {
            m_TypeInfos = TypeManager.GetTypeInfoDataPointer();
            m_EntityOffsetInfos = TypeManager.GetEntityOffsetsPointer();
        }

        public ref readonly TypeManager.TypeInfo GetTypeInfo(TypeIndex typeIndex)
        {
            return ref *TypeManager.GetTypeInfoPointer(m_TypeInfos, typeIndex);
        }

        public TypeManager.EntityOffsetInfo* GetEntityOffsets(in TypeManager.TypeInfo typeInfo)
        {
            return m_EntityOffsetInfos + typeInfo.EntityOffsetStartIndex;
        }

        public TypeIndex ChunkComponentToNormalTypeIndex(TypeIndex typeIndex) => TypeManager.GetTypeInfoPointer(m_TypeInfos, typeIndex)->TypeIndex;

        public static void Destroy(EntityComponentStore* entityComponentStore)
        {
            entityComponentStore->Dispose();
        }

        void Dispose()
        {


            if (m_ComponentTypeOrderVersion != null)
            {
                Memory.Unmanaged.Free(m_ComponentTypeOrderVersion, Allocator.Persistent);
                m_ComponentTypeOrderVersion = null;
            }

            // Move all chunks to become pooled chunks
            for (var i = 0; i < m_Archetypes.Length; i++)
            {
                var archetype = m_Archetypes.Ptr[i];


                for (int c = 0; c != archetype->Chunks.Count; c++)
                {
                    var chunk = archetype->Chunks[c];

                    ChunkDataUtility.DeallocateBuffers(archetype, chunk);

                    var entityCount = chunk.Count;

                    s_entityStore.Data.DeallocateEntities((Entity*)chunk.Buffer, entityCount);

                    s_chunkStore.Data.FreeContiguousChunks(archetype->Chunks[c], 1);
                }

                archetype->Chunks.Dispose();
                archetype->ChunksWithEmptySlots.Dispose();
                archetype->FreeChunksBySharedComponents.Dispose();
                archetype->MatchingQueryData.Dispose();
            }

            var entitiesWithNames = m_NameStoreAccess.GetEntitiesWithNamesRO();
            foreach (var e in entitiesWithNames)
            {
                EntityNameStorage.RemoveEntitiesFromNameMap(&e, 1);
            }
            m_NameStoreAccess.Dispose();

            m_Archetypes.Dispose();

            m_TypeLookup.Dispose();
            m_ArchetypeChunkAllocator.Dispose();
            CompanionChangesTracker.Dispose();

            foreach (var kv in m_UnmanagedSharedComponentsByType)
            {
                var components = kv.Value;
                var typeInfo = TypeManager.GetTypeInfoPointer(kv.Key);
                var typeIndexWithFlags = typeInfo->TypeIndex;
                /*
                 * if it implements irefcounted, we should go through every live instance of this type that we have
                 * and call Release() on it
                 */
                if (typeIndexWithFlags.IsRefCounted)
                {
                    var size = typeInfo->TypeSize;

                    /*
                     * the info list for this type may have been cleared by ResetSharedComponentData, which means
                     * this entitycomponentstore no longer owns any refcounts for that typeindex. so we only try to
                     * release stuff that we have nonzero refcounts for. the type may also never have gotten an info
                     * list at all, because GetSharedComponentDataAddr_Unmanaged creates the component list on its
                     * own when asked for a default value.
                     */
                    if (m_UnmanagedSharedComponentInfo.TryGetValue(kv.Key, out var infos) && infos.IsCreated)
                    {
                        for (int j = 1; j < math.min(components.Length, infos.Length); j++)
                        {
                            if (infos[j].RefCount > 0)
                                TypeManager.CallIRefCounted_Release(typeIndexWithFlags, (IntPtr) components.Ptr + j * size);
                        }
                    }
                }
                components.Dispose();
            }
            m_UnmanagedSharedComponentsByType.Dispose();

            foreach (var kv in m_UnmanagedSharedComponentInfo)
            {
                kv.Value.Dispose();
            }
            m_UnmanagedSharedComponentInfo.Dispose();
            m_UnmanagedSharedComponentTypes.Dispose();
            m_HashLookup.Dispose();

            m_StructuralChangesRecorder->Flush();
            m_StructuralChangesRecorder->Dispose();
            Memory.Unmanaged.Free(m_StructuralChangesRecorder, Allocator.Persistent);
            m_StructuralChangesRecorder = null;
        }

        private void ResetSharedComponentData()
        {
            /*
             * we reset shared component data when we have moved all our shared components from this world to another
             * world. when that happens, we expect each shared component type index to return to having exactly one
             * entry representing the default value of that shared component. so we clear the lists for each typeindex,
             * and reset it to that state.
             */

            // KVPair<,>.Value hands back a ref into the map's value array, so these mutate in place.
            foreach (var kv in m_UnmanagedSharedComponentsByType)
            {
                kv.Value.Length = 1;
            }

            foreach (var kv in m_UnmanagedSharedComponentInfo)
            {
                kv.Value.Clear();
                kv.Value.Add(new SharedComponentInfo
                    {RefCount = 1, ComponentType = -1, Version = 1, HashCode = 0});
            }
            m_HashLookup.Clear();
        }

        public void FreeAllEntities(
            )
        {
            var archetypes = m_Archetypes;

            for (int archetypeIndex = 0, archetypeCount = archetypes.Length;
                 archetypeIndex < archetypeCount;
                 archetypeIndex++)
            {
                var archetype = m_Archetypes.Ptr[archetypeIndex];

                for (int chunkIndex = 0, chunkCount = archetype->Chunks.Count; chunkIndex < chunkCount; chunkIndex++)
                {
                    var chunk = archetype->Chunks[chunkIndex];
                    var entities = (Entity*)chunk.Buffer;
                    var entityCount = chunk.Count;
                    s_entityStore.Data.DeallocateEntities(entities, entityCount);
                }
                var entitiesWithNames = m_NameStoreAccess.GetEntitiesWithNamesRO();
                foreach (var e in entitiesWithNames)
                {
                    EntityNameStorage.RemoveEntitiesFromNameMap(&e, 1);
                }
            }

            m_NameStoreAccess.ResetEntitiesWithNames();
        }

        public void FreeEntities(ChunkIndex chunk)
        {
            var count = chunk.Count;
            var entities = (Entity*)chunk.Buffer;
            s_entityStore.Data.DeallocateEntities(entities, count);

            // We need to remove the Entity with name from the set
            m_NameStoreAccess.RemoveEntitiesFromEntitiesWithNames(entities, count);
            EntityNameStorage.RemoveEntitiesFromNameMap(entities, count);

        }

        public string GetName(Entity entity)
        {
            if (!Exists(entity))
                return "ENTITY_NOT_FOUND";
            return EntityNameStorage.GetEntityName(entity).ToString();

        }

        [GenerateTestsForBurstCompatibility]
        public void GetName(Entity entity, out FixedString64Bytes name)
        {
            if (!Exists(entity))
            {
                name = "ENTITY_NOT_FOUND";
                return;
            }
            name = default;
            EntityNameStorage.GetEntityName(entity).ToFixedString(ref name);

        }

        public static string Debugger_GetName(EntityComponentStore* store, Entity entity)
        {

            if (store != null && store->m_NameStoreAccess.IsCreated && !Debugger_Exists(store, entity))
                return "ENTITY_NOT_FOUND";
            return EntityNameStorage.GetEntityName(entity).ToString();

        }

        [GenerateTestsForBurstCompatibility]
        public void SetName(Entity entity, in FixedString64Bytes name)
        {
            if (!Exists(entity))
                return;

            var entityName = new EntityName();
            entityName.SetFixedString(in name);
            EntityNameStorage.SetEntityName(entity, entityName);
            m_NameStoreAccess.AddEntityToEntitiesWithNames(entity);
        }

        public void CopyName(Entity dstEntity, Entity srcEntity)
        {

            var entityName = EntityNameStorage.GetEntityName(srcEntity);
            EntityNameStorage.SetEntityName(dstEntity, entityName);
            if (entityName.Index != 0)
                m_NameStoreAccess.AddEntityToEntitiesWithNames(dstEntity);
        }

        public Archetype* GetArchetype(Entity entity)
        {
            AssertEntitiesExist(&entity, 1);
            var chunk = GetChunk(entity);
            Assert.IsTrue(chunk != ChunkIndex.Null);
            return GetArchetype(chunk);
        }

        public ChunkIndex GetChunk(Entity entity)
        {
            return s_entityStore.Data.GetEntityInChunk(entity).Chunk;
        }

        [BurstCompile]
        internal partial struct PerChunkArray
        {
            public struct PerChunkData
            {
                public Archetype* Archetype;
                public int EntityCount;
                public int ListIndex;
            }

            PerChunkData* m_PerChunkData;

            [OnCodeUnloading]
            static void OnCodeUnloading()
            {
                if (StaticIdentifier.Ref.Data.m_PerChunkData != null)
                {
                    var memoryLabel = Memory.CreateLabel("Entities", "Chunks.Metadata", Allocator.Persistent);
                    Memory.Unmanaged.Free(StaticIdentifier.Ref.Data.m_PerChunkData, memoryLabel);
                    StaticIdentifier.Ref.Data.m_PerChunkData = null;
                }
            }

            [BurstDiscard]
            internal static void Initialize()
            {
                if (StaticIdentifier.Ref.Data.m_PerChunkData == null)
                {
                    var memoryLabel = Memory.CreateLabel("Entities", "Chunks.Metadata", Allocator.Persistent);
                    var size = sizeof(PerChunkData) * ChunkStore.kMaximumChunkCount;
                    var data = (PerChunkData*)Memory.Unmanaged.Allocate(size, CollectionHelper.CacheLineSize, memoryLabel);
                    UnsafeUtility.MemClear(data, size);
                    StaticIdentifier.Ref.Data.m_PerChunkData = data;
                }
            }

            [EntitiesCleanup]
            static void Shutdown()
            {
                var memoryLabel = Memory.CreateLabel("Entities", "Chunks.Metadata", Allocator.Persistent);
                Memory.Unmanaged.Free(StaticIdentifier.Ref.Data.m_PerChunkData, memoryLabel);
                StaticIdentifier.Ref.Data.m_PerChunkData = null;
            }

            sealed class StaticIdentifier
            {
                internal static readonly SharedStatic<PerChunkArray> Ref = SharedStatic<PerChunkArray>.GetOrCreate<StaticIdentifier>();
            }

            public static PerChunkData* ChunkData => StaticIdentifier.Ref.Data.m_PerChunkData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Archetype* GetArchetype(ChunkIndex chunk)
        {
            return PerChunkArray.ChunkData[chunk].Archetype;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetArchetype(ChunkIndex chunk, Archetype* archetype)
        {
            PerChunkArray.ChunkData[chunk].Archetype = archetype;
        }

        public void SetEntityInChunk(Entity entity, EntityInChunk entityInChunk)
        {
            s_entityStore.Data.SetEntityInChunk(entity, entityInChunk);
        }

        public EntityInChunk GetEntityInChunk(Entity entity)
        {
            return s_entityStore.Data.GetEntityInChunk(entity);
        }

        public void IncrementComponentTypeOrderVersion(Archetype* archetype)
        {
            // Increment type component version
            for (var t = 0; t < archetype->TypesCount; ++t)
            {
                var typeIndex = archetype->Types[t].TypeIndex;
                m_ComponentTypeOrderVersion[typeIndex.Index]++;
            }
        }

        public bool Exists(Entity entity)
        {
            ValidateEntity(entity);

            if(!s_entityStore.Data.Exists(entity))
                return false;

            var chunk = GetChunk(entity);
            // A null chunk means the entity was pre-allocated by an EntityCommandBuffer but not
            // yet played back. In the old per-world entity store this never showed as "alive";
            // with the global EntityIdStore it can. Return false (entity not yet in this world).
            if (chunk == ChunkIndex.Null)
                return false;

            var archetype = GetArchetype(chunk);
            var ecs = archetype->EntityComponentStore;

            return ecs->WorldSequenceNumber == WorldSequenceNumber;
        }

        public static bool Debugger_Exists(EntityComponentStore* store, Entity entity)
        {
            return s_entityStore.Data.Exists(entity);
        }


        public int GetComponentTypeOrderVersion(TypeIndex typeIndex)
        {
            return m_ComponentTypeOrderVersion[typeIndex.Index];
        }

        public bool HasComponent(Entity entity, TypeIndex type, out bool entityExists)
        {
            entityExists = Exists(entity);
            if (Hint.Unlikely(!entityExists))
                return false;

            var archetype = GetArchetype(entity);
            return ChunkDataUtility.GetIndexInTypeArray(archetype, type) != -1;
        }

        internal bool HasComponent(Entity entity, TypeIndex type, ref LookupCache cache, out bool entityExists)
        {
            entityExists = Exists(entity);
            if (Hint.Unlikely(!entityExists))
                return false;

            var archetype = GetArchetype(entity);
            if (Hint.Unlikely(archetype != cache.Archetype))
                cache.Update(archetype, type);
            return cache.IndexInArchetype != -1;
        }

        public bool HasComponent(Entity entity, ComponentType type, out bool entityExists)
        {
            entityExists = Exists(entity);
            if (Hint.Unlikely(!entityExists))
                return false;

            var archetype = GetArchetype(entity);
            return ChunkDataUtility.GetIndexInTypeArray(archetype, type.TypeIndex) != -1;
        }

        public void SetChunkComponent<T>(NativeArray<ArchetypeChunk> chunks, T componentData)
            where T : unmanaged, IComponentData
        {
            var type = ComponentType.ReadWrite<T>();
            var chunkTypeIndex = TypeManager.MakeChunkComponentTypeIndex(type.TypeIndex);
            ArchetypeChunk* chunkPtr = (ArchetypeChunk*)NativeArrayUnsafeUtility.GetUnsafePtr(chunks);

            SetChunkComponent(chunkPtr, chunks.Length, &componentData, chunkTypeIndex);
        }

        public void SetChunkComponent(ArchetypeChunk* chunks, int chunkCount, void* componentData, TypeIndex componentTypeIndex)
        {
            var type = ComponentType.FromTypeIndex(componentTypeIndex);
            if (type.IsZeroSized)
                return;

            for (int i = 0; i < chunkCount; i++)
            {
                var srcChunk = chunks[i].m_Chunk;
                var ptr = GetComponentDataWithTypeRW(srcChunk.MetaChunkEntity, componentTypeIndex, m_GlobalSystemVersion);
                var sizeInChunk = GetTypeInfo(componentTypeIndex).SizeInChunk;
                UnsafeUtility.MemCpy(ptr, componentData, sizeInChunk);
            }
        }

        public void GetChunk(Entity entity, out ChunkIndex chunk, out int chunkIndex)
        {
            var entityInChunk = GetEntityInChunk(entity);

            chunk = entityInChunk.Chunk;
            chunkIndex = entityInChunk.IndexInChunk;
        }

        public byte* GetComponentDataWithTypeRO(Entity entity, TypeIndex typeIndex)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var entityChunk = entityInChunk.Chunk;
            var entityIndexInChunk = entityInChunk.IndexInChunk;
            var archetype = GetArchetype(entityChunk);

            return ChunkDataUtility.GetComponentDataWithTypeRO(entityChunk, archetype, entityIndexInChunk, typeIndex);
        }

        public static byte* Debugger_GetComponentDataWithTypeRO(EntityComponentStore* store, Entity entity, TypeIndex typeIndex)
        {
            if (!Debugger_Exists(store, entity))
                return null;

            var entityInChunk = store->GetEntityInChunk(entity);
            var entityChunk = entityInChunk.Chunk;
            var entityIndexInChunk = entityInChunk.IndexInChunk;
            var entityArchetype = store->GetArchetype(entityChunk);

            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(entityArchetype, typeIndex);
            if (indexInTypeArray == -1)
                return null;

            return ChunkDataUtility.GetComponentDataRO(entityChunk, entityArchetype, entityIndexInChunk, indexInTypeArray);
        }

        public byte* GetComponentDataWithTypeRW(Entity entity, TypeIndex typeIndex, uint globalVersion)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var entityChunk = entityInChunk.Chunk;
            var entityIndexInChunk = entityInChunk.IndexInChunk;
            var archetype = GetArchetype(entityChunk);

            var data = ChunkDataUtility.GetComponentDataWithTypeRW(entityChunk, archetype, entityIndexInChunk, typeIndex,
                globalVersion);

            return data;
        }

        // This method will return an invalid pointer if the entity does not have the provided type. It is the caller's
        // responsibility to ensure that the type exists on the entity.
        public byte* GetComponentDataWithTypeRO(Entity entity, TypeIndex typeIndex, ref LookupCache cache)
        {
            var entityInChunk = GetEntityInChunk(entity);
            return ChunkDataUtility.GetComponentDataWithTypeRO(entityInChunk.Chunk, GetArchetype(entityInChunk.Chunk), entityInChunk.IndexInChunk, typeIndex, ref cache);
        }

        // This method does NOT check the input entity for validity/existence. It is the caller's responsibility to confirm
        // the input entity exists.
        // This method will return a null pointer if the entity does not have the provided type.
        public byte* GetOptionalComponentDataWithTypeRO(Entity entity, TypeIndex typeIndex, ref LookupCache cache)
        {
            var entityInChunk = GetEntityInChunk(entity);
            return ChunkDataUtility.GetOptionalComponentDataWithTypeRO(entityInChunk.Chunk, GetArchetype(entityInChunk.Chunk), entityInChunk.IndexInChunk, typeIndex, ref cache);
        }

        // This method will return an invalid pointer if the entity does not have the provided type. It is the caller's
        // responsibility to ensure that the type exists on the entity.
        public byte* GetComponentDataWithTypeRW(Entity entity, TypeIndex typeIndex, uint globalVersion, ref LookupCache cache)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var data = ChunkDataUtility.GetComponentDataWithTypeRW(entityInChunk.Chunk, GetArchetype(entityInChunk.Chunk), entityInChunk.IndexInChunk, typeIndex, globalVersion, ref cache);

            return data;
        }

        // This method does NOT check the input entity for validity/existence. It is the caller's responsibility to confirm
        // the input entity exists.
        // This method will return a null pointer if the entity does not have the provided type.
        public byte* GetOptionalComponentDataWithTypeRW(Entity entity, TypeIndex typeIndex, uint globalVersion, ref LookupCache cache)
        {
            var entityInChunk = GetEntityInChunk(entity);
            var data = ChunkDataUtility.GetOptionalComponentDataWithTypeRW(entityInChunk.Chunk, GetArchetype(entityInChunk.Chunk), entityInChunk.IndexInChunk, typeIndex, globalVersion, ref cache);

            return data;
        }

        public void* GetComponentDataRawRW(Entity entity, TypeIndex typeIndex)
        {
            AssertEntityHasComponent(entity, typeIndex);
            return GetComponentDataRawRWEntityHasComponent(entity, typeIndex);
        }

        internal void* GetComponentDataRawRWEntityHasComponent(Entity entity, TypeIndex typeIndex)
        {
            AssertNotZeroSizedComponent(typeIndex);
            return GetComponentDataWithTypeRW(entity, typeIndex, GlobalSystemVersion);
        }

        public void SetComponentDataRawEntityHasComponent(Entity entity, TypeIndex typeIndex, void* data, int size)
        {
            AssertComponentSizeMatches(typeIndex, size);
            var ptr = GetComponentDataWithTypeRW(entity, typeIndex,
                GlobalSystemVersion);
            UnsafeUtility.MemCpy(ptr, data, size);
        }

        public void SetBufferRaw(Entity entity, TypeIndex componentTypeIndex, BufferHeader* tempBuffer, int sizeInChunk)
        {
            var ptr = GetComponentDataWithTypeRW(entity, componentTypeIndex,
                GlobalSystemVersion);

            BufferHeader.Destroy((BufferHeader*)ptr);

            UnsafeUtility.MemCpy(ptr, tempBuffer, sizeInChunk);
        }

        [GenerateTestsForBurstCompatibility]
        public int GetSharedComponentDataIndex(Entity entity, TypeIndex typeIndex)
        {
            var archetype = GetArchetype(entity);
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);
            var chunk = GetChunk(entity);
            var sharedComponentValueArray = archetype->Chunks.GetSharedComponentValues(chunk.ListIndex);
            var sharedComponentOffset = indexInTypeArray - archetype->FirstSharedComponent;
            return sharedComponentValueArray[sharedComponentOffset];
        }

        public int Debugger_GetSharedComponentDataIndex(Entity entity, TypeIndex typeIndex)
        {
            var archetype = GetArchetype(entity);
            var indexInTypeArray = ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex);
            if (indexInTypeArray == -1)
                return -1;

            var chunk = GetChunk(entity);
            var sharedComponentValueArray = archetype->Chunks.GetSharedComponentValues(chunk.ListIndex);
            var sharedComponentOffset = indexInTypeArray - archetype->FirstSharedComponent;
            return sharedComponentValueArray[sharedComponentOffset];
        }


        public void AddExistingEntitiesInChunk(ChunkIndex chunk)
        {
            var archetype = GetArchetype(chunk);
            for (int iEntity = 0, count = chunk.Count; iEntity < count; ++iEntity)
            {
                var entity = (Entity*)ChunkDataUtility.GetComponentDataRO(chunk, archetype, iEntity, 0);

                s_entityStore.Data.SetEntityInChunk(*entity, new EntityInChunk { Chunk = chunk, IndexInChunk = iEntity });
                s_entityStore.Data.SetEntityVersion(*entity, entity->Version);
            }
        }

        public void AllocateEntitiesForRemapping(EntityComponentStore* srcEntityComponentStore, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            var archetypes = srcEntityComponentStore->m_Archetypes;

            for (int archetypeIndex = 0, archetypeCount = archetypes.Length; archetypeIndex < archetypeCount; archetypeIndex++)
            {
                var archetype = archetypes.Ptr[archetypeIndex];

                for (int chunkIndex = 0, chunkCount = archetype->Chunks.Count; chunkIndex < chunkCount; chunkIndex++)
                {
                    var chunk = archetype->Chunks[chunkIndex];
                    AllocateEntitiesForRemapping(chunk, srcEntityComponentStore, ref entityRemapping);
                }
            }
        }

        public void AllocateEntitiesForRemapping(ChunkIndex chunk, EntityComponentStore* srcComponentStore, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            var srcEntities = (Entity*)chunk.Buffer;
            var entityCount = chunk.Count;
            var dstEntities = stackalloc Entity[TypeManager.MaximumChunkCapacity];

            s_entityStore.Data.AllocateEntitiesDirect(dstEntities, entityCount, ChunkIndex.Null, 0);

            for (int entityIndex = 0; entityIndex < entityCount; entityIndex++)
            {
                EntityRemapUtility.AddEntityRemapping(ref entityRemapping,
                    srcEntities[entityIndex],
                    dstEntities[entityIndex]);

                CopyName(dstEntities[entityIndex], srcEntities[entityIndex]);
            }
        }

        public void RemapChunk(ChunkIndex chunk, int count, ref NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping)
        {
            var entityInChunkStart = (Entity*)chunk.Buffer;

            for (var i = 0; i != count; i++)
            {
                var entityInChunk = entityInChunkStart + i;
                var target = EntityRemapUtility.RemapEntity(ref entityRemapping, *entityInChunk);

                *entityInChunk = target;
                SetEntityInChunk(target, new EntityInChunk { Chunk = chunk, IndexInChunk = i });
            }

            if (chunk.MetaChunkEntity != Entity.Null)
            {
                chunk.MetaChunkEntity = EntityRemapUtility.RemapEntity(ref entityRemapping, chunk.MetaChunkEntity);
            }
        }

        // There is a special case where the index is 0, we interpret it as an unmanaged value even though it may not, but it's ok because 0 is always interpreted as "don't do anything" and
        //  it's better to favor branching in the unmanaged path rather than the managed one.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsUnmanagedSharedComponentIndex(int sharedComponentIndex) =>
            (sharedComponentIndex == 0) || (sharedComponentIndex & kUnmanagedSharedComponentIndexFlag) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static TypeIndex GetComponentTypeFromSharedComponentIndex(int sharedComponentIndex)
        {
            var typeIndexNoFlags = (sharedComponentIndex & ~kUnmanagedSharedComponentIndexFlag) >> kUnmanagedSharedTypeIndexBitOffset;
            return TypeManager.GetTypeInfoPointer(typeIndexNoFlags)->TypeIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetElementIndexFromSharedComponentIndex(int sharedComponentIndex) =>
            sharedComponentIndex & kUnmanagedSharedElementIndexMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int BuildUnmanagedSharedComponentDataIndex(int elementIndex, TypeIndex typeIndex)
        {
            if(elementIndex > kUnmanagedSharedElementIndexMask)
                throw new ArgumentException($"Too many unique values for shared component {TypeManager.GetTypeInfo(typeIndex).DebugTypeName}. Trying building the data indes for a shared component with value at index {elementIndex} but only {kUnmanagedSharedElementIndexMask + 1} unique values for a given shared component type can be present at the same time.");
            return kUnmanagedSharedComponentIndexFlag | (typeIndex.Value << kUnmanagedSharedTypeIndexBitOffset) | elementIndex;
        }


#pragma warning disable 0618 //Untyped UnsafeList is obsolete.
        // May insert into the map, so only structural change paths may call this. Readers use TryGetSharedComponentDataAddr_Unmanaged.
        private ComponentTypeList* CheckGetSharedComponentList(TypeIndex typeIndex)
        {
            ref var map = ref m_UnmanagedSharedComponentsByType.m_Data;
            var typeIndexNoFlags = typeIndex.Index;

            var entry = map.Find(typeIndexNoFlags);
            if (entry == -1)
            {
                // GetTypeInfoPointer clamps an unregistered type index to entry 0 instead of failing, so
                // an invalid index would mint a map entry with TypeSize 0 that later lookups cannot tell
                // apart from a real one. Validate before TryAdd so a bad index never gets in.
                var typeInfo = TypeManager.GetTypeInfoPointer(typeIndex);
                if (typeInfo->TypeIndex.Index != typeIndexNoFlags)
                {
                    throw new ArgumentException($"Invalid type index {typeIndex}", nameof(typeIndex));
                }
                var typeSize = typeInfo->TypeSize;
                entry = map.TryAdd(typeIndexNoFlags);
                var created = (ComponentTypeList*)map.Ptr + entry;
                *created = new ComponentTypeList(typeSize, 16, 16 * typeSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                created->Length = 1;
                return created;
            }

            return (ComponentTypeList*)map.Ptr + entry;
        }
#pragma warning restore 0618

        private UnsafeList<SharedComponentInfo>* CheckGetSharedComponentInfo(TypeIndex typeIndex)
        {
            ref var map = ref m_UnmanagedSharedComponentInfo.m_Data;
            var typeIndexNoFlags = typeIndex.Index;

            // Keyed by the typeIndex, so one list per shared component type
            var entry = map.Find(typeIndexNoFlags);
            if (entry == -1)
            {
                // See CheckGetSharedComponentList: validate before TryAdd, because an unregistered
                // type index would otherwise be added to the map and never rejected again.
                if (TypeManager.GetTypeInfoPointer(typeIndex)->TypeIndex.Index != typeIndexNoFlags)
                {
                    throw new ArgumentException($"Invalid type index {typeIndex}", nameof(typeIndex));
                }
                entry = map.TryAdd(typeIndexNoFlags);
                var created = (UnsafeList<SharedComponentInfo>*)map.Ptr + entry;
                *created = new UnsafeList<SharedComponentInfo>(16, Allocator.Persistent, NativeArrayOptions.ClearMemory);
                // First entry is reserved for default value, ComponentType is used as the entry point for the Free linked list
                created->Add(new SharedComponentInfo {RefCount = 1, HashCode = 0, Version = 1, ComponentType = -1});
                m_UnmanagedSharedComponentTypes.Add(typeIndex);
                return created;
            }

            return (UnsafeList<SharedComponentInfo>*)map.Ptr + entry;
        }

        private int FindSharedComponentIndex(
            TypeIndex typeIndex,
            int hashCode,
            void* newData,
            void* defaultValue,
            out UnsafeList<SharedComponentInfo>* infos,
#pragma warning disable 0618 //Untyped UnsafeList is obsolete.
            out ComponentTypeList* components)
#pragma warning restore 0618
        {
            components = CheckGetSharedComponentList(typeIndex);
            infos = CheckGetSharedComponentInfo(typeIndex);

            // If newData is null, we assume we want to find the default value, so return 0
            if (newData == null)
            {
                return 0;
            }

            // If defaultValue is null, we assume we are looking for a non-default SharedComponent
            if (defaultValue != null && TypeManager.SharedComponentEquals(newData, defaultValue, typeIndex))
            {
                return 0;
            }

            return FindNonDefaultSharedComponentIndex(typeIndex, hashCode, newData, out infos, out components);
        }

        private int FindNonDefaultSharedComponentIndex(TypeIndex typeIndex, int hashCode, void* newData, out UnsafeList<SharedComponentInfo>* infos, out ComponentTypeList* components)
        {
            // It's unlikely a hash computation produces 0, and if it's the case, we will end up...computing it again
            if (hashCode == 0)
            {
                hashCode = TypeManager.SharedComponentGetHashCode(newData, typeIndex);
            }

            int itemIndex;
            NativeParallelMultiHashMapIterator<ulong> iter;

            if (!m_HashLookup.TryGetFirstValue(GetSharedComponentHashKey(typeIndex, hashCode), out itemIndex, out iter))
            {
                infos = null;
                components = null;
                return -1;
            }

            var typeSize = TypeManager.GetTypeInfo(typeIndex).TypeSize;
            components = CheckGetSharedComponentList(typeIndex);
            infos = CheckGetSharedComponentInfo(typeIndex);
            do
            {
                if (TypeManager.SharedComponentEquals((byte*) components->Ptr + (itemIndex * typeSize), newData, typeIndex))
                {
                    return itemIndex;
                }
            }
            while (m_HashLookup.TryGetNextValue(out itemIndex, ref iter));

            return -1;
        }

        internal static ulong GetSharedComponentHashKey(TypeIndex typeIndex, int hashCode)
        {
            /*
             * make sure the type index has flags, so that the key will always be the same whether you pass in a
             * typeindex with or without flags
             */
            var indexForKey = TypeManager.GetTypeInfoPointer(typeIndex)->TypeIndex;
            var key = (((ulong)(uint)(int)indexForKey) << 32) | (ulong)(uint)hashCode;
            return key;
        }

        internal int CloneSharedComponentNonDefault(EntityComponentStore* srcComponentStore, int sharedComponentIndex)
        {
            var typeIndex = GetComponentTypeFromSharedComponentIndex(sharedComponentIndex);
            var componentDataAddr = srcComponentStore->GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
            var elementIndex = GetElementIndexFromSharedComponentIndex(sharedComponentIndex);
            var infos = srcComponentStore->CheckGetSharedComponentInfo(typeIndex);

            return InsertSharedComponent_Unmanaged(typeIndex, infos->Ptr[elementIndex].HashCode, componentDataAddr, null);
        }

        // If defaultValue is null we assume we are inserting a non-default value
        internal int InsertSharedComponent_Unmanaged(TypeIndex typeIndex, int hashCode, void* data, void* defaultValue)
        {
            // It's unlikely a hash computation produces 0, and if it's the case, we will end up...computing it again
            if (hashCode == 0)
            {
                // No data, means we're inserting the default value, we will exit before we need a hash
                hashCode = data!=null ? TypeManager.SharedComponentGetHashCode(data, typeIndex) : 0;
            }

            var elementIndex = FindSharedComponentIndex(
                typeIndex,
                hashCode,
                data,
                defaultValue,
                out var infos,
                out var components);

            // We return 0 as the sharedComponentIndex for the default value, 0 is interpreted by the code paths as "don't do anything",
            //  which is the case for the default value, whether it's managed or unmanaged.
            if (elementIndex == 0)
                return 0;

            if (elementIndex != -1)
            {
                infos->Ptr[elementIndex].RefCount++;
                return BuildUnmanagedSharedComponentDataIndex(elementIndex, typeIndex);
            }

            if (typeIndex.IsRefCounted)
            {
                TypeManager.CallIRefCounted_Retain(typeIndex, (IntPtr)data);
            }

            var typeSize = TypeManager.GetTypeInfo(typeIndex).TypeSize;

            infos = CheckGetSharedComponentInfo(typeIndex);
            components = CheckGetSharedComponentList(typeIndex);
            Assert.AreEqual(components->Length, infos->Length);

            var info = new SharedComponentInfo { RefCount = 1, HashCode = hashCode, ComponentType = typeIndex.Value, Version = m_SharedComponentVersion++ };

            // Check if we can use an entry that was previously freed
            elementIndex = infos->Ptr[0].ComponentType;
            if (elementIndex != -1)
            {
                infos->Ptr[0].ComponentType = infos->Ptr[elementIndex].ComponentType;
                UnsafeUtility.MemCpy((byte*)components->Ptr + elementIndex*typeSize, data, typeSize);
                UnsafeUtility.WriteArrayElement(infos->Ptr, elementIndex, info);
            }

            // Allocate a new entry
            else
            {
                elementIndex = components->Length;
                components->Resize(typeSize, 16, components->Length + 1);
                UnsafeUtility.MemCpy((byte*)components->Ptr + elementIndex*typeSize, data, typeSize);

                infos->Add(info);
            }

            m_HashLookup.Add(GetSharedComponentHashKey(typeIndex, hashCode), elementIndex);
            m_UnmanagedSharedComponentCount++;

            return BuildUnmanagedSharedComponentDataIndex(elementIndex, typeIndex);
        }

        [GenerateTestsForBurstCompatibility]
        internal int GetUnmanagedSharedComponentCount()
        {
            return m_UnmanagedSharedComponentCount;
        }

        [GenerateTestsForBurstCompatibility]
        internal void AddSharedComponentReference_Unmanaged(int sharedComponentIndex, int numRefs = 1)
        {
            var elementIndex = GetElementIndexFromSharedComponentIndex(sharedComponentIndex);
            if (elementIndex == 0) return;

            var componentTypeIndex = GetComponentTypeFromSharedComponentIndex(sharedComponentIndex);
            var infos = CheckGetSharedComponentInfo(componentTypeIndex);

            infos->Ptr[elementIndex].RefCount += numRefs;
        }

        internal void GetSharedComponentData_Unmanaged(int sharedComponentIndex, TypeIndex typeIndex, void* destinationAddress)
        {
            var typeSize = TypeManager.GetTypeInfo(typeIndex).TypeSize;
            var sourceAddress = TryGetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
            if (sourceAddress == null)
            {
                // No list means no value of this type was stored, so only the all-zero default exists.
                UnsafeUtility.MemClear(destinationAddress, typeSize);
                return;
            }
            UnsafeUtility.MemCpy(destinationAddress, sourceAddress, typeSize);
        }

        // sharedComponentIndex can be 0, in which case we return the first entry of the type which corresponds to the default value
        internal void* GetSharedComponentDataAddr_Unmanaged(int sharedComponentIndex, TypeIndex typeIndex)
        {
            var address = TryGetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
            // Types in an archetype get their list in CreateArchetype, so any index read from a chunk resolves.
            if (address == null && typeIndex != TypeIndex.Null)
                Assert.IsTrue(false, $"componentTypeIndex {typeIndex.Index} has no shared component list");
            return address;
        }

        // Jobs call this concurrently, so it must only read the shared component maps and never insert into them.
        // Returns null if the type has no list in this store.
        private void* TryGetSharedComponentDataAddr_Unmanaged(int sharedComponentIndex, TypeIndex typeIndex)
        {
            // if typeindex is Null, don't bother with all this checking, because nothing matters and everything
            // will definitely be null. we should clean this up so we don't make this call or many of its parents
            // when typeindex is Null.
            if (typeIndex == TypeIndex.Null)
                return null;

            var elementIndex = GetElementIndexFromSharedComponentIndex(sharedComponentIndex);
            var typeIndexNoFlags = typeIndex.Index;

            ref var map = ref m_UnmanagedSharedComponentsByType.m_Data;
            var entry = map.Find(typeIndexNoFlags);
            if (entry == -1)
            {
                // A non-default index can only come from InsertSharedComponent_Unmanaged, which creates the list.
                if (sharedComponentIndex != 0)
                    Assert.IsTrue(false, $"componentTypeIndex {typeIndexNoFlags} has no shared component list");
                return null;
            }

            var components = (ComponentTypeList*)map.Ptr + entry;
            UnityEngine.Assertions.AssertCore.IsTrue(components->IsCreated && components->Length > 0);

            var typeSize = TypeManager.GetTypeInfo(typeIndex).TypeSize;
            if (elementIndex >= components->Length)
            {
                Assert.IsTrue(false, $"elementIndex {elementIndex} is out of range {components->Length}");
            }
            return (byte*)components->Ptr + elementIndex*typeSize;
        }

        internal object GetSharedComponentDataObject_Unmanaged(int sharedComponentIndex, TypeIndex typeIndex)
        {
            // A null address constructs the default value.
            var componentAddr = TryGetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, typeIndex);
            return TypeManager.ConstructComponentFromBuffer(typeIndex, componentAddr);
        }

        [GenerateTestsForBurstCompatibility]
        internal void RemoveSharedComponentReference_Unmanaged(int sharedComponentIndex, int numRefs = 1)
        {
            var componentTypeIndex = GetComponentTypeFromSharedComponentIndex(sharedComponentIndex);
            var elementIndex = GetElementIndexFromSharedComponentIndex(sharedComponentIndex);
            if (elementIndex == 0)
                return;

            var infos = CheckGetSharedComponentInfo(componentTypeIndex);

            var newCount = infos->Ptr[elementIndex].RefCount -= numRefs;
            Assert.IsTrue(newCount >= 0);

            if (newCount != 0)
                return;
            /*
             * we only call release() for IRefCounted things when our own separate refcount goes to 0.
             */
            if (componentTypeIndex.IsRefCounted)
            {
                var data = GetSharedComponentDataAddr_Unmanaged(sharedComponentIndex, componentTypeIndex);
                TypeManager.CallIRefCounted_Release(componentTypeIndex, (IntPtr)data);
            }
            // Bump default version when a shared component is removed completely.
            // This ensures that when asking for a shared component that previously existed and no longer exists
            // It will always return a change value.
            IncrementSharedComponentVersion_Unmanaged(0);

            var hashCode = infos->Ptr[elementIndex].HashCode;

            // Update the free linked list by adding the entry we just freed
            infos->Ptr[elementIndex].ComponentType = infos->Ptr[0].ComponentType;
            infos->Ptr[0].ComponentType = elementIndex;

            int itemIndex;
            NativeParallelMultiHashMapIterator<ulong> iter;

            if (m_HashLookup.TryGetFirstValue(GetSharedComponentHashKey(componentTypeIndex, hashCode), out itemIndex, out iter))
            {
                do
                {
                    if (itemIndex == elementIndex)
                    {
                        m_HashLookup.Remove(iter);
                        m_UnmanagedSharedComponentCount--;
                        return;
                    }
                }
                while (m_HashLookup.TryGetNextValue(out itemIndex, ref iter));
            }

            throw new System.InvalidOperationException("shared component couldn't be removed due to internal state corruption");
        }

        [GenerateTestsForBurstCompatibility]
        internal void IncrementSharedComponentVersion_Unmanaged(int sharedComponentIndex)
        {
            var version = ++m_SharedComponentVersion;
            if (sharedComponentIndex == 0)
            {
                m_SharedComponentGlobalVersion = version;
            }
            else
            {
                var componentType = GetComponentTypeFromSharedComponentIndex(sharedComponentIndex);
                var elementIndex = GetElementIndexFromSharedComponentIndex(sharedComponentIndex);
                var info = CheckGetSharedComponentInfo(componentType);

                info->Ptr[elementIndex].Version = version;
            }
        }

        internal int GetSharedComponentVersion_Unmanaged(TypeIndex typeIndex, void* sharedData, void* defaultData)
        {
            var elementIndex = FindSharedComponentIndex(typeIndex, 0, sharedData, defaultData, out var infos, out _);
            if (elementIndex <= 0)
            {
                return m_SharedComponentGlobalVersion;
            }
            return infos->Ptr[elementIndex].Version;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] {typeof(BurstCompatibleSharedComponentData)})]
        internal void GetAllUniqueSharedComponents_Unmanaged<T>(out UnsafeList<T> sharedComponentValues, AllocatorManager.AllocatorHandle allocator) where T : unmanaged, ISharedComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var defaultValue = default(T);
            var components = CheckGetSharedComponentList(typeIndex);
            var infos = CheckGetSharedComponentInfo(typeIndex);
            var typeSize = TypeManager.GetTypeInfo<T>().TypeSize;

            sharedComponentValues = new UnsafeList<T>(components->Length, allocator);
            sharedComponentValues.Add(defaultValue);

            //0 is always default
            for (int i = 1, j = 1; i < infos->Length; i++)
            {
                if (infos->Ptr[i].RefCount > 0)
                {
                    UnsafeUtility.MemCpy((byte*)sharedComponentValues.Ptr + j*typeSize, (byte*)components->Ptr + i*typeSize, typeSize);
                    sharedComponentValues.Length++;
                    j++;
                }
            }
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        internal void GetAllUniqueSharedComponents_Unmanaged<T>(
            out UnsafeList<T> sharedComponentValues,
            out UnsafeList<int> sharedComponentIndices,
            AllocatorManager.AllocatorHandle allocator) where T : unmanaged, ISharedComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var defaultValue = default(T);
            var components = CheckGetSharedComponentList(typeIndex);
            var infos = CheckGetSharedComponentInfo(typeIndex);
            var typeSize = TypeManager.GetTypeInfo<T>().TypeSize;

            sharedComponentValues = new UnsafeList<T>(components->Length, allocator);
            sharedComponentIndices = new UnsafeList<int>(components->Length, allocator);
            sharedComponentValues.Add(defaultValue);
            sharedComponentIndices.Add(0);

            //0 is always default
            for (int i = 1, j = 1; i < infos->Length; i++)
            {
                if (infos->Ptr[i].RefCount > 0)
                {
                    UnsafeUtility.MemCpy((byte*)sharedComponentValues.Ptr + j * typeSize, (byte*)components->Ptr + i * typeSize, typeSize);
                    sharedComponentValues.Length++;
                    sharedComponentIndices.Add(BuildUnmanagedSharedComponentDataIndex(i, typeIndex));
                    j++;
                }
            }
        }

#pragma warning disable 0618 //Untyped UnsafeList is obsolete.

#pragma warning restore 0618

        internal void MoveAllSharedComponents_Unmanaged(EntityComponentStore* srcEntityComponentStore, ref NativeParallelHashMap<int, int> result)
        {
            var sharedComponentTypeCount = srcEntityComponentStore->m_UnmanagedSharedComponentTypes.Length;
            for (int i = 0; i < sharedComponentTypeCount; ++i)
            {
                var typeIndex = srcEntityComponentStore->m_UnmanagedSharedComponentTypes[i];
                var typeSize = TypeManager.GetTypeInfo(typeIndex).TypeSize;

                var srcComponents = srcEntityComponentStore->CheckGetSharedComponentList(typeIndex);
                var srcInfos = srcEntityComponentStore->CheckGetSharedComponentInfo(typeIndex);

                for (int j = 1; j < srcComponents->Length; j++)
                {
                    // TODO: Strip all the flags from the typeindex as we don't want to do anything special based on the
                    // typeindex  such as Retain()/Release() an additional time since we are specifically in the
                    // MoveEntities codepath were we want components// to be identical in the dst world as the src world
                    // DOTS-6895
                    // The IEquatableTypeFlag is kept because hashing and equality branch on it.
                    var dstIndex = InsertSharedComponent_Unmanaged(new TypeIndex() { Value = typeIndex.Index | (typeIndex.Value & TypeManager.IEquatableTypeFlag) }, srcInfos->Ptr[j].HashCode, (byte*) srcComponents->Ptr + j * typeSize, null);
                    AddSharedComponentReference_Unmanaged(dstIndex, srcInfos->Ptr[j].RefCount - 1);
                    IncrementSharedComponentVersion_Unmanaged(dstIndex);

                    result.Add(BuildUnmanagedSharedComponentDataIndex(j, typeIndex), dstIndex);
                }
            }

            srcEntityComponentStore->ResetSharedComponentData();
        }

        public bool AllSharedComponentReferencesAreFromChunks(UnsafeParallelHashMap<int, int> refCountMap)
        {
            var sharedComponentTypeCount = m_UnmanagedSharedComponentTypes.Length;
            for (int i = 0; i < sharedComponentTypeCount; ++i)
            {
                var typeIndex = m_UnmanagedSharedComponentTypes[i];
                var infos = CheckGetSharedComponentInfo(typeIndex);

                for (int j = 1; j < infos->Length; j++)
                {
                    var sharedComponentIndex = BuildUnmanagedSharedComponentDataIndex(j, typeIndex);
                    if (refCountMap.TryGetValue(sharedComponentIndex, out var recordedRefCount))
                    {
                        var infoRefCount = infos->Ptr[j].RefCount;
                        if (infoRefCount != recordedRefCount)
                            return false;
                    }
                }
            }

            return true;
        }

        [BurstCompile]
        private static void EntityBatchFromEntityChunkDataShared(EntityComponentStore* ecs, in EntityInChunk* chunkData,
            int chunkCount,
            EntityBatchInChunk* entityBatchList, int* currentbatchIndex,
            int nSharedComponentsToAdd, int* foundError)
        {

            *foundError = 0;

            var entityIndex = 0;
            var entityBatch = new EntityBatchInChunk
            {
                Chunk = chunkData[entityIndex].Chunk,
                StartIndex = chunkData[entityIndex].IndexInChunk,
                Count = 1
            };
            entityIndex++;
            while (entityIndex < chunkCount)
            {
                // Skip this entity if it's a duplicate.  Checking previous entityIndex is sufficient
                // since arrays are sorted.
                if (chunkData[entityIndex].Equals(chunkData[entityIndex - 1]))
                {
                    entityIndex++;
                    continue;
                }

                var chunk = chunkData[entityIndex].Chunk;
                var indexInChunk = chunkData[entityIndex].IndexInChunk;
                var chunkBreak = (chunk != entityBatch.Chunk);
                var indexBreak = (indexInChunk != (entityBatch.StartIndex + entityBatch.Count));
                var runBreak = chunkBreak || indexBreak;
                if (runBreak && entityBatch.Chunk != ChunkIndex.Null)
                {

                    var entityBatchArchetype = ecs->GetArchetype(entityBatch.Chunk);
                    if (nSharedComponentsToAdd + entityBatchArchetype->NumSharedComponents > kMaxSharedComponentCount)
                    {
                        *foundError = 1;
                        return;
                    }

                    entityBatchList[*currentbatchIndex] = entityBatch;
                    *currentbatchIndex = *currentbatchIndex + 1;
                    //just to make sure we do not overflow our entityBatchList buffer
                    if (*currentbatchIndex > chunkCount)
                    {
                        *foundError = 1;
                        return;
                    }

                    entityBatch = new EntityBatchInChunk
                    {
                        Chunk = chunk,
                        StartIndex = indexInChunk,
                        Count = 1
                    };
                }
                else
                {
                    entityBatch = new EntityBatchInChunk
                    {
                        Chunk = entityBatch.Chunk,
                        StartIndex = entityBatch.StartIndex,
                        Count = entityBatch.Count + 1
                    };
                }

                entityIndex++;
            }

            if (entityBatch.Chunk == ChunkIndex.Null)
                return;

            // Deleted Entity (not an error)
            if (nSharedComponentsToAdd + ecs->GetArchetype(entityBatch.Chunk)->NumSharedComponents > kMaxSharedComponentCount)
            {
                *foundError = 1;
                return;
            }

            entityBatchList[*currentbatchIndex] = entityBatch;
            *currentbatchIndex = *currentbatchIndex + 1;
            //just to make sure we do not overflow our entityBatchList buffer
            if (*currentbatchIndex > chunkCount)
            {
                *foundError = 1;
            }
        }

        [BurstCompile]
        private static void SortEntityInChunk(EntityInChunk* entityInChunks, int count)
        {
            NativeSortExtension.Sort(entityInChunks,count);
        }

        [BurstCompile]
        private static void GatherEntityInChunkForEntities(Entity* Entities,
            EntityInChunk* EntityChunkData, int numEntities)
        {
            for (int index = 0; index < numEntities; ++index)
            {
                var entity = Entities[index];
                EntityChunkData[index] = s_entityStore.Data.GetEntityInChunk(entity);
            }
        }

        internal bool CreateEntityBatchList(NativeArray<Entity> entities, int nSharedComponentsToAdd,
            AllocatorManager.AllocatorHandle allocator, out NativeList<EntityBatchInChunk> entityBatchList)
        {
            if (entities.Length == 0)
            {
                entityBatchList = default;
                return false;
            }

            var entityChunkData = new NativeArray<EntityInChunk>(entities.Length, Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory);

            GatherEntityInChunkForEntities((Entity*) entities.GetUnsafeReadOnlyPtr(),
                (EntityInChunk*) entityChunkData.GetUnsafePtr(),entities.Length);

            SortEntityInChunk((EntityInChunk*)entityChunkData.GetUnsafePtr(), entityChunkData.Length);

            entityBatchList = new NativeList<EntityBatchInChunk>(entityChunkData.Length, allocator);
            entityBatchList.Length = entityChunkData.Length;

            int foundError = 0;
            int finalBatchSize = 0;

            fixed (EntityComponentStore* ecs = &this)
            {
                EntityBatchFromEntityChunkDataShared(ecs, (EntityInChunk*)entityChunkData.GetUnsafePtr(),
                    entityChunkData.Length, (EntityBatchInChunk*)entityBatchList.GetUnsafePtr(),&finalBatchSize,
                    nSharedComponentsToAdd,&foundError);
            }

            entityBatchList.Length = finalBatchSize;

            entityChunkData.Dispose();
            if (foundError != 0)
            {
                entityBatchList.Dispose();
                entityBatchList = default;
                return false;
            }

            return true;
        }


        public ulong AllocateSequenceNumber()
        {
            // EntityComponentStore.CheckInternalConsistency checks for a chunk's SequenceNumber to be != 0
            // So we want to be sure that we increment the global sequence number before returning it, so
            // it will never be zero.
            // The following two lines used to be inverted, and we hit an instability that was happening
            // when running a single test that creates a single entity, inside an EcsTestFixture.
            m_NextChunkSequenceNumber++;
            var sequenceNumber = m_NextChunkSequenceNumber;
            return sequenceNumber;
        }

#pragma warning disable 169
        struct Ulong16
        {
            private ulong p00;
            private ulong p01;
            private ulong p02;
            private ulong p03;
            private ulong p04;
            private ulong p05;
            private ulong p06;
            private ulong p07;
            private ulong p08;
            private ulong p09;
            private ulong p10;
            private ulong p11;
            private ulong p12;
            private ulong p13;
            private ulong p14;
            private ulong p15;
        }

        struct Ulong256
        {
            private Ulong16 p00;
            private Ulong16 p01;
            private Ulong16 p02;
            private Ulong16 p03;
            private Ulong16 p04;
            private Ulong16 p05;
            private Ulong16 p06;
            private Ulong16 p07;
            private Ulong16 p08;
            private Ulong16 p09;
            private Ulong16 p10;
            private Ulong16 p11;
            private Ulong16 p12;
            private Ulong16 p13;
            private Ulong16 p14;
            private Ulong16 p15;
            public int Length { get { return 256; } set {} }
            public ref long ElementAt(int index)
            {
                fixed(Ulong16* p = &p00) { return ref UnsafeUtility.AsRef<long>((long*)p + index); }
            }
        }

        struct Ulong4096
        {
            private Ulong256 p00;
            private Ulong256 p01;
            private Ulong256 p02;
            private Ulong256 p03;
            private Ulong256 p04;
            private Ulong256 p05;
            private Ulong256 p06;
            private Ulong256 p07;
            private Ulong256 p08;
            private Ulong256 p09;
            private Ulong256 p10;
            private Ulong256 p11;
            private Ulong256 p12;
            private Ulong256 p13;
            private Ulong256 p14;
            private Ulong256 p15;
        }

       struct Ulong16384 : IIndexable<long>
        {
            private Ulong4096 p00;
            private Ulong4096 p01;
            private Ulong4096 p02;
            private Ulong4096 p03;
            public int Length { get { return 16384; } set {} }
            public ref long ElementAt(int index)
            {
                fixed(Ulong4096* p = &p00) { return ref UnsafeUtility.AsRef<long>((long*)p + index); }
            }
        }
#pragma warning restore 169

        internal struct ChunkStore : IDisposable
        {
            Ulong16384 m_megachunk;
            Ulong16384 m_chunkInUse;
            Ulong256   m_megachunkIsFull;
            static readonly int log2ChunksPerMegachunk = 6;
            static readonly int chunksPerMegachunk = 1 << log2ChunksPerMegachunk;
            static readonly int log2MegachunksInUniverse = 14;
            static readonly int megachunksInUniverse = 1 << log2MegachunksInUniverse;
            public static readonly int kMaximumChunkCount = 1 << (log2ChunksPerMegachunk + log2MegachunksInUniverse);

            public static readonly int kErrorNone = 0;
            public static readonly int kErrorAllocationFailed = -1;
            public static readonly int kErrorChunkAlreadyFreed = -2;
            public static readonly int kErrorChunkAlreadyMarkedFree = -3;
            public static readonly int kErrorChunkNotFound = -4;
            public static readonly int kErrorNoChunksAvailable = -5;

            static readonly int ChunkSizeInBytesRoundedUpToPow2 = math.ceilpow2(Chunk.kChunkSize);
            static readonly int Log2ChunkSizeInBytesRoundedUpToPow2 = math.tzcnt(ChunkSizeInBytesRoundedUpToPow2);
            static readonly int MegachunkSizeInBytes = 1 << (Log2ChunkSizeInBytesRoundedUpToPow2 + 6);

            public static void Initialize()
            {
                // Mark first mega chunk as used so chunk index 0 can be used as a Null value
                s_chunkStore.Data.m_chunkInUse.ElementAt(0) = ~0L;
                s_chunkStore.Data.m_megachunkIsFull.ElementAt(0) = 1L;
            }

            int AllocationFailed(int offset, int count)
            {
                int error = ConcurrentMask.TryFree(ref m_chunkInUse, offset, count);
                if(error == ConcurrentMask.ErrorFailedToFree)
                    return kErrorChunkAlreadyFreed;
                return kErrorAllocationFailed;
            }

            internal Chunk* GetChunkPointer(int chunkIndex)
            {
                var megachunkIndex = chunkIndex >> log2ChunksPerMegachunk;
                var chunkInMegachunk = chunkIndex & (chunksPerMegachunk-1);
                var megachunk = (byte*)m_megachunk.ElementAt(megachunkIndex);
                var chunk = megachunk + (chunkInMegachunk << Log2ChunkSizeInBytesRoundedUpToPow2);
                return (Chunk*)chunk;
            }

            public int AllocateContiguousChunks(out ChunkIndex value, int requestedCount, out int actualCount)
            {
                int gigachunkIndex = 0;
                for(; gigachunkIndex < m_megachunkIsFull.Length; ++gigachunkIndex)
                    if(m_megachunkIsFull.ElementAt(gigachunkIndex) != ~0L)
                        break;
                int firstMegachunk = gigachunkIndex << 6;
                actualCount = math.min(chunksPerMegachunk, requestedCount); // literally can't service requests for more
                value = ChunkIndex.Null;
                while(actualCount > 0)
                {
                    for(int offset = 0; offset < megachunksInUniverse; ++offset)
                    {
                        int megachunkIndex = (firstMegachunk + offset) & (megachunksInUniverse-1); // index of current megachunk
                        long maskAfterAllocation, oldMask, newMask, readMask = m_chunkInUse.ElementAt(megachunkIndex); // read the mask of which chunks are allocated
                        int chunkInMegachunk; // index of first chunk allocated in current megachunk
                        do {
                            oldMask = readMask;
                            if(oldMask == ~0L)
                                goto NEXT_MEGACHUNK; // can't find any bits, try the next megachunk
                            if(!ConcurrentMask.foundAtLeastThisManyConsecutiveZeroes(oldMask, actualCount, out chunkInMegachunk, out int _)) // find consecutive 0 bits to allocate into
                                goto NEXT_MEGACHUNK; // can't find enough bits, try the next megachunk
                            newMask = maskAfterAllocation = oldMask | ConcurrentMask.MakeMask(chunkInMegachunk, actualCount); // mask in the freshly allocated bits
                            if(oldMask == 0L) // if we're the first to allocate from this megachunk,
                                newMask = ~0L; // mark the whole megachunk as full (busy) until we're done allocating memory
                            readMask = Interlocked.CompareExchange(ref m_chunkInUse.ElementAt(megachunkIndex), newMask, oldMask);
                        } while(readMask != oldMask);
                        int firstChunkIndex = (megachunkIndex << log2ChunksPerMegachunk) + chunkInMegachunk;
                        if(oldMask == 0L) // if we are the first allocation in this chunk...
                        {
                            long allocated = (long)Memory.Unmanaged.Allocate(MegachunkSizeInBytes, CollectionHelper.CacheLineSize, Allocator.Persistent); // allocate memory
                            if (allocated == 0L) // if the allocation failed...
                                return AllocationFailed(firstChunkIndex, actualCount);
                            Interlocked.Exchange(ref m_megachunk.ElementAt(megachunkIndex), allocated); // store the pointer to the freshly allocated memory
                            Interlocked.Exchange(ref m_chunkInUse.ElementAt(megachunkIndex), maskAfterAllocation); // change the mask from ~0L to the true mask after our allocation (which may be ~0L)
                        }
                        if(maskAfterAllocation == ~0L)
                            ConcurrentMask.AtomicOr(ref m_megachunkIsFull.ElementAt(megachunkIndex>>6), 1L << (megachunkIndex & 63));
                        value = new ChunkIndex(firstChunkIndex);
                        return kErrorNone;
                        NEXT_MEGACHUNK:;
                    }
                    actualCount >>= 1;
                }
                return kErrorNoChunksAvailable;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
            void ThrowChunkAlreadyMarkedAsFree(ChunkIndex chunk)
            {
                throw new ArgumentException($"Chunk index {(int)chunk} (address {(long)chunk.GetPtr():x8}) already marked as free");
            }

            public int FreeContiguousChunks(ChunkIndex firstChunk, int count)
            {
                var megachunkIndex = firstChunk >> log2ChunksPerMegachunk;
                var chunkIndexInMegachunk = firstChunk & (chunksPerMegachunk - 1);

                long chunksToFree = ConcurrentMask.MakeMask(chunkIndexInMegachunk, count);
                long oldMask, newMask, readMask = m_chunkInUse.ElementAt(megachunkIndex); // read the mask of which chunks are allocated
                do
                {
                    oldMask = readMask;
                    if((oldMask & chunksToFree) != chunksToFree) // if any of our chunks were already freed,
                    {
                        ThrowChunkAlreadyMarkedAsFree(firstChunk); // pretty serious error! throw,
                        return kErrorChunkAlreadyMarkedFree; // and return an error code.
                    }
                    newMask = oldMask & ~chunksToFree; // zero out the chunks to free in the mask
                    if(newMask == 0L) // if this would zero out the whole mask,
                        newMask = ~0L; // *set* the whole mask.. to block new allocations from other threads until we can free the memory
                    readMask = Interlocked.CompareExchange(ref m_chunkInUse.ElementAt(megachunkIndex), newMask, oldMask);
                } while (readMask != oldMask);
                if(newMask == ~0L) // we set the whole mask, we aren't done until we free the memory and then zero the whole mask.
                {
                    var oldMegachunk = Interlocked.Exchange(ref m_megachunk.ElementAt(megachunkIndex), 0L); // set the pointer to 0.
                    Interlocked.Exchange(ref m_chunkInUse.ElementAt(megachunkIndex), 0L); // set the word to 0. "come allocate from me!"
                    Memory.Unmanaged.Free((byte*)oldMegachunk, Allocator.Persistent); // free the megachunk, since nobody can see it anymore.
                }
                ConcurrentMask.AtomicAnd(ref m_megachunkIsFull.ElementAt(megachunkIndex>>6), ~(1L << (megachunkIndex & 63)));
                return kErrorNone;
            }

            public void Dispose()
            {
                for(var megachunkIndex = 0; megachunkIndex < megachunksInUniverse; ++megachunkIndex)
                {
                    void* megachunk = (void*)m_megachunk.ElementAt(megachunkIndex);
                    if(megachunk != null)
                        Memory.Unmanaged.Free(megachunk, Allocator.Persistent);
                }
                this = default;
            }
        }

        internal static readonly SharedStatic<ChunkStore> s_chunkStore = SharedStatic<ChunkStore>.GetOrCreate<EntityComponentStore>();

        public static int AllocateContiguousChunk(int requestedCount, out ChunkIndex chunk, out int actualCount)
        {
            return s_chunkStore.Data.AllocateContiguousChunks(out chunk, requestedCount, out  actualCount);
        }

        public static int FreeContiguousChunks(ChunkIndex firstChunk, int count)
        {
            return s_chunkStore.Data.FreeContiguousChunks(firstChunk, count);
        }

        public ChunkIndex AllocateChunk()
        {
            if(s_chunkStore.Data.AllocateContiguousChunks(out var newChunk, 1, out _) != ChunkStore.kErrorNone)
            {
                throw new InvalidOperationException($"ChunkStore.AllocateContiguousChunks failed");
            }

            if (useMemoryInitPattern != 0)
            {
                var raw = newChunk.GetPtr();
                UnsafeUtility.MemSet(raw, memoryInitPattern, Chunk.kChunkSize);
            }

            return newChunk;
        }

        public void FreeChunk(ChunkIndex chunk)
        {
            var success = s_chunkStore.Data.FreeContiguousChunks(chunk, 1);
            Assert.IsTrue(success == 0);
        }

        public Archetype* GetExistingArchetype(ComponentTypeInArchetype* typesSorted, int count)
        {
            return m_TypeLookup.TryGet(typesSorted, count);
        }

        void ChunkAllocate<T>(void* pointer, int count = 1) where T : struct
        {
            void** pointerToPointer = (void**)pointer;
            *pointerToPointer =
                m_ArchetypeChunkAllocator.Allocate(UnsafeUtility.SizeOf<T>() * count, UnsafeUtility.AlignOf<T>());
        }

        internal static int GetComponentArraySize(int componentSize, int entityCount) => CollectionHelper.Align(componentSize * entityCount, CollectionHelper.CacheLineSize);

        static int CalculateSpaceRequirement(ushort* componentSizes, int componentCount, int entityCount)
        {
            int size = 0;
            for (int i = 0; i < componentCount; ++i)
                size += GetComponentArraySize(componentSizes[i], entityCount);
            return size;
        }

        static int CalculateChunkCapacity(int bufferSize, ushort* componentSizes, int count)
        {
            int totalSize = 0;
            for (int i = 0; i < count; ++i)
                totalSize += componentSizes[i];

            int capacity = bufferSize / totalSize;
            while (CalculateSpaceRequirement(componentSizes, count, capacity) > bufferSize)
                --capacity;
            return capacity;
        }

        static ulong CalculateStableHash(ComponentTypeInArchetype* types, int* typeMemoryOrder, int count)
        {
            ulong hash = 1;
            for (var i = 0; i < count; ++i)
            {
                var typeIndex = types[typeMemoryOrder[i]].TypeIndex;
                var stableTypeIndex = TypeManager.GetTypeInfo(typeIndex).StableTypeHash;
                hash = TypeHash.CombineFNV1A64(hash, stableTypeIndex);
            }
            return hash;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowIfComponentDataSizeIsLargerThanShortMaxValue(int sizeInChunk)
        {
            if (sizeInChunk > short.MaxValue)
                throw new ArgumentException($"Component Data sizes may not be larger than {short.MaxValue}");
        }

        internal Archetype* CreateArchetype(ComponentTypeInArchetype* types, int count)
        {
            AssertArchetypeComponents(types, count);

            // Compute how many IComponentData types store Entities and need to be patched.
            // Types can have more than one entity, which means that this count is not necessarily
            // the same as the type count.
            var scalarEntityPatchCount = 0;
            var bufferEntityPatchCount = 0;
            var numSharedComponents = 0;
            var enableableComponentIndexInArchetype = stackalloc int[count];
            var numEnableableComponents = 0;

            for (var i = 0; i < count; ++i)
            {
                ref readonly var ct = ref GetTypeInfo(types[i].TypeIndex);

                if (ct.Category == TypeManager.TypeCategory.ISharedComponentData)
                {
                    ++numSharedComponents;
                }
                else if (types[i].TypeIndex.IsManagedComponentInternal)
                {
                    throw new ArgumentException("UnityEngine.Object types cannot be added to entities. Use CompanionComponent<T> to associate a UnityEngine.Component with an entity.");
                }
                else
                {
                    if (ct.BufferCapacity >= 0)
                        bufferEntityPatchCount += ct.EntityOffsetCount;
                    else if (ct.SizeInChunk > 0)
                        scalarEntityPatchCount += ct.EntityOffsetCount;
                }

                if (types[i].IsEnableable)
                    enableableComponentIndexInArchetype[numEnableableComponents++] = i;
            }

            Archetype* dstArchetype = null;
            ChunkAllocate<Archetype>(&dstArchetype);
            ChunkAllocate<ComponentTypeInArchetype>(&dstArchetype->Types, count);
            ChunkAllocate<int>(&dstArchetype->EnableableTypeIndexInArchetype, numEnableableComponents);
            ChunkAllocate<int>(&dstArchetype->Offsets, count);
            ChunkAllocate<int>(&dstArchetype->SizeOfs, count);
            ChunkAllocate<int>(&dstArchetype->BufferCapacities, count);
            ChunkAllocate<int>(&dstArchetype->TypeMemoryOrderIndexToIndexInArchetype, count);
            ChunkAllocate<int>(&dstArchetype->TypeIndexInArchetypeToMemoryOrderIndex, count);
            ChunkAllocate<EntityRemapUtility.EntityPatchInfo>(&dstArchetype->ScalarEntityPatches, scalarEntityPatchCount);
            ChunkAllocate<EntityRemapUtility.BufferEntityPatchInfo>(&dstArchetype->BufferEntityPatches, bufferEntityPatchCount);

            dstArchetype->TypesCount = count;
            Memory.Array.Copy(dstArchetype->Types, types, count);
            dstArchetype->EnableableTypesCount = numEnableableComponents;
            Memory.Array.Copy(dstArchetype->EnableableTypeIndexInArchetype, (int*)enableableComponentIndexInArchetype, numEnableableComponents);
            dstArchetype->EntityCount = 0;
            dstArchetype->ChunksWithEmptySlots = new UnsafeList<ChunkIndex>(0, Allocator.Persistent);
            dstArchetype->MatchingQueryData = new UnsafeList<IntPtr>(0, Allocator.Persistent);
            dstArchetype->NextChangedArchetype = null;
            dstArchetype->InstantiateArchetype = null;
            dstArchetype->CopyArchetype = null;
            dstArchetype->MetaChunkArchetype = null;
            dstArchetype->CleanupResidueArchetype = null;

            dstArchetype->Flags = 0;

            {
                short i = (short)count;
                do dstArchetype->FirstChunkComponent = i;
                while (types[--i].IsChunkComponent);
                i++;
                do dstArchetype->FirstSharedComponent = i;
                while (types[--i].IsSharedComponent);
                i++;
                do dstArchetype->FirstTagComponent = i;
                while (types[--i].IsZeroSized);
                i++;
                do dstArchetype->FirstBufferComponent = i;
                while (types[--i].IsBuffer);
            }

            // Reads never create shared component lists, and GetSharedComponentDataAddr_Unmanaged must return a
            // valid default value address for any type a chunk can hold, so the list is created with the archetype.
            for (int i = dstArchetype->FirstSharedComponent; i < dstArchetype->FirstChunkComponent; ++i)
            {
                var typeIndex = types[i].TypeIndex;
                CheckGetSharedComponentList(typeIndex);
                CheckGetSharedComponentInfo(typeIndex);
            }

            ulong bloomFilterMask = 0;
            for (var i = 0; i < count; ++i)
            {
                var typeIndex = types[i].TypeIndex;
                ref readonly var typeInfo = ref GetTypeInfo(typeIndex);
                bloomFilterMask |= typeInfo.BloomFilterMask;
                if (typeIndex == m_DisabledType)
                    dstArchetype->Flags |= ArchetypeFlags.Disabled;
                if (typeIndex == m_SystemInstanceType)
                    dstArchetype->Flags |= ArchetypeFlags.HasSystemInstanceComponents;
                if (typeIndex == m_PrefabType)
                    dstArchetype->Flags |= ArchetypeFlags.Prefab;
                if (typeIndex == m_ChunkHeaderType)
                    dstArchetype->Flags |= ArchetypeFlags.HasChunkHeader;
                if (typeInfo.HasBlobAssetRefs)
                    dstArchetype->Flags |= ArchetypeFlags.HasBlobAssetRefs;
                if (typeIndex == CompanionComponentStore.CompanionLinkTypeIndex)
                    dstArchetype->Flags |= ArchetypeFlags.HasCompanionComponents;
                if (typeInfo.HasWeakAssetRefs)
                    dstArchetype->Flags |= ArchetypeFlags.HasWeakAssetRefs;
                if (typeInfo.HasUnityObjectRefs)
                    dstArchetype->Flags |= ArchetypeFlags.HasUnityObjectRefs;
                if ((typeInfo.TypeIndex & TypeManager.HasOnAddedCallbackFlag) != 0)
                    dstArchetype->Flags |= ArchetypeFlags.HasOnAddedCallbacks;
                if ((typeInfo.TypeIndex & TypeManager.HasOnRemovedCallbackFlag) != 0)
                    dstArchetype->Flags |= ArchetypeFlags.HasOnRemovedCallbacks;
            }
            dstArchetype->BloomFilterMask = bloomFilterMask;

            if (dstArchetype->NumBufferComponents > 0)
                dstArchetype->Flags |= ArchetypeFlags.HasBufferComponents;


            var chunkDataSize = Chunk.kChunkBufferSize;

            dstArchetype->ScalarEntityPatchCount = scalarEntityPatchCount;
            dstArchetype->BufferEntityPatchCount = bufferEntityPatchCount;

            int maxCapacity = TypeManager.MaximumChunkCapacity;
            for (var i = 0; i < count; ++i)
            {
                ref readonly var cType = ref GetTypeInfo(types[i].TypeIndex);
                if (i < dstArchetype->NonZeroSizedTypesCount)
                {
                    ThrowIfComponentDataSizeIsLargerThanShortMaxValue(cType.SizeInChunk);
                    dstArchetype->SizeOfs[i] = (ushort)cType.SizeInChunk;
                    dstArchetype->BufferCapacities[i] = cType.BufferCapacity;
                }
                else
                {
                    dstArchetype->SizeOfs[i] = 0;
                    dstArchetype->BufferCapacities[i] = 0;
                }
                maxCapacity = math.min(maxCapacity, cType.MaximumChunkCapacity);
            }

            dstArchetype->ChunkCapacity = math.min(maxCapacity, CalculateChunkCapacity(chunkDataSize, dstArchetype->SizeOfs, dstArchetype->NonZeroSizedTypesCount));
            Assert.IsTrue(dstArchetype->ChunkCapacity > 0);
            Assert.IsTrue(TypeManager.MaximumChunkCapacity >= dstArchetype->ChunkCapacity);
            dstArchetype->Chunks = new ArchetypeChunkData(count, numSharedComponents);

            dstArchetype->InstanceSize = 0;
            dstArchetype->InstanceSizeWithOverhead = 0;
            for (var i = 0; i < dstArchetype->NonZeroSizedTypesCount; ++i)
            {
                dstArchetype->InstanceSize += dstArchetype->SizeOfs[i];
                dstArchetype->InstanceSizeWithOverhead += GetComponentArraySize(dstArchetype->SizeOfs[i], 1);
            }

            // For serialization a stable ordering of the components in the
            // chunk is desired. The type index is not stable, since it depends
            // on the order in which types are added to the TypeManager.
            // A permutation of the types ordered by a TypeManager-generated
            // memory ordering is used instead.
            var memoryOrderings = stackalloc UInt64[count];
            var typeFlags = stackalloc int[count];

            for (int i = 0; i < count; ++i)
            {
                var typeIndex = types[i].TypeIndex;
                memoryOrderings[i] = GetTypeInfo(typeIndex).MemoryOrdering;
                typeFlags[i] = typeIndex.Flags;
            }

            // Having memory order depend on type flags has the advantage that
            // TypeMemoryOrderIndexToIndexInArchetype is stable within component types
            // i.e. if Types[X] is a buffer component then Types[TypeMemoryOrderIndexToIndexInArchetype[X]] is also a buffer component
            // this simplifies iterating types in memory order (mainly serialization code)
            bool MemoryOrderCompare(int lhs, int rhs)
            {
                if (typeFlags[lhs] == typeFlags[rhs])
                    return memoryOrderings[lhs] < memoryOrderings[rhs];
                return typeFlags[lhs] < typeFlags[rhs];
            }

            for (int i = 0; i < count; ++i)
            {
                int index = i;
                while (index > 1 && MemoryOrderCompare(i, dstArchetype->TypeMemoryOrderIndexToIndexInArchetype[index - 1]))
                {
                    dstArchetype->TypeMemoryOrderIndexToIndexInArchetype[index] = dstArchetype->TypeMemoryOrderIndexToIndexInArchetype[index - 1];
                    --index;
                }
                dstArchetype->TypeMemoryOrderIndexToIndexInArchetype[index] = i;
            }
            for (int i = 0; i < count; ++i)
            {
                dstArchetype->TypeIndexInArchetypeToMemoryOrderIndex[dstArchetype->TypeMemoryOrderIndexToIndexInArchetype[i]] = i;
            }

            var usedBytes = 0;
            for (var typeMemoryOrderIndex = 0; typeMemoryOrderIndex < count; ++typeMemoryOrderIndex)
            {
                var indexInArchetype = dstArchetype->TypeMemoryOrderIndexToIndexInArchetype[typeMemoryOrderIndex];
                var sizeOf = dstArchetype->SizeOfs[indexInArchetype];

                // align usedBytes upwards (eating into alignExtraSpace) so that
                // this component actually starts at its required alignment.
                // Assumption is that the start of the entire data segment is at the
                // maximum possible alignment.
                dstArchetype->Offsets[indexInArchetype] = usedBytes;
                usedBytes += GetComponentArraySize(sizeOf, dstArchetype->ChunkCapacity);
            }

            // Fill in arrays of scalar and buffer entity patches
            var scalarPatchInfo = dstArchetype->ScalarEntityPatches;
            var bufferPatchInfo = dstArchetype->BufferEntityPatches;
            for (var i = 0; i != count; i++)
            {
                ref readonly var ct = ref GetTypeInfo(types[i].TypeIndex);
                var offsets = GetEntityOffsets(ct);
                var offsetCount = ct.EntityOffsetCount;

                if (ct.BufferCapacity >= 0)
                {
                    bufferPatchInfo = EntityRemapUtility.AppendBufferEntityPatches(bufferPatchInfo, offsets, offsetCount, dstArchetype->Offsets[i], dstArchetype->SizeOfs[i], ct.ElementSize);
                }
                else if (ct.SizeInChunk > 0)
                {
                    scalarPatchInfo = EntityRemapUtility.AppendEntityPatches(scalarPatchInfo, offsets, offsetCount, dstArchetype->Offsets[i], dstArchetype->SizeOfs[i]);
                }
            }
            Assert.AreEqual(scalarPatchInfo - dstArchetype->ScalarEntityPatches, scalarEntityPatchCount);

            dstArchetype->ScalarEntityPatchCount = scalarEntityPatchCount;
            dstArchetype->BufferEntityPatchCount = bufferEntityPatchCount;
            UnsafeUtility.MemClear(dstArchetype->QueryMaskArray, sizeof(byte) * 128);

            // Update the list of all created archetypes
            m_Archetypes.Add(dstArchetype);

            dstArchetype->FreeChunksBySharedComponents = new ChunkListMap();
            dstArchetype->FreeChunksBySharedComponents.Init(dstArchetype, 16);

            m_TypeLookup.Add(dstArchetype);

            if (ArchetypeCleanupComplete(dstArchetype))
                dstArchetype->Flags |= ArchetypeFlags.CleanupComplete;
            if (ArchetypeCleanupNeeded(dstArchetype))
                dstArchetype->Flags |= ArchetypeFlags.CleanupNeeded;

            fixed(EntityComponentStore* entityComponentStore = &this)
            {
                dstArchetype->EntityComponentStore = entityComponentStore;
            }

            dstArchetype->StableHash = CalculateStableHash(types, dstArchetype->TypeMemoryOrderIndexToIndexInArchetype, count);

            EntitiesProfilerPackageCalls.ArchetypeAdded(dstArchetype);

            return dstArchetype;
        }

        private bool ArchetypeCleanupComplete(Archetype* archetype)
        {
            return archetype->TypesCount == 2 && archetype->Types[1].TypeIndex == m_CleanupEntityType;
        }

        private bool ArchetypeCleanupNeeded(Archetype* archetype)
        {
            for (var t = 1; t < archetype->TypesCount; ++t)
            {
                var type = archetype->Types[t];
                if (type.IsCleanupComponent)
                {
                    return true;
                }
            }

            return false;
        }

        public int CountEntities()
        {
            int entityCount = 0;
            for (var i = 0; i < m_Archetypes.Length; ++i)
            {
                var archetype = m_Archetypes.Ptr[i];
                entityCount += archetype->EntityCount;
            }

            return entityCount;
        }

        public struct ArchetypeChanges
        {
            public int StartIndex;
            public uint ArchetypeTrackingVersion;
        }

        public ArchetypeChanges BeginArchetypeChangeTracking()
        {
            m_ArchetypeTrackingVersion++;
            return new ArchetypeChanges
            {
                StartIndex = m_Archetypes.Length,
                ArchetypeTrackingVersion = m_ArchetypeTrackingVersion
            };
        }

        public void EndArchetypeChangeTracking(ArchetypeChanges changes, EntityQueryManager* queries)
        {
            if (m_Archetypes.Length - changes.StartIndex == 0)
                return;

            Assert.AreEqual(m_ArchetypeTrackingVersion, changes.ArchetypeTrackingVersion);
            var changeList = new UnsafePtrList<Archetype>(m_Archetypes.Ptr + changes.StartIndex, m_Archetypes.Length - changes.StartIndex);
            queries->AddAdditionalArchetypes(changeList);
        }


        internal struct ChunkListChanges
        {
            public Archetype* ArchetypeTrackingHead;

            public void Init()
            {
                ArchetypeTrackingHead = null;
            }

            public void TrackArchetype(Archetype* archetype)
            {
                if (archetype->NextChangedArchetype == null)
                {
                    archetype->NextChangedArchetype = ArchetypeTrackingHead;
                    ArchetypeTrackingHead = archetype;
                }
            }
        }

        public void InvalidateChunkListCacheForChangedArchetypes()
        {
            var archetype = m_ChunkListChangesTracker.ArchetypeTrackingHead;
            while(archetype != null)
            {
                var matchingQueryCount = archetype->MatchingQueryData.Length;
                for (int queryIndex = 0; queryIndex < matchingQueryCount; ++queryIndex)
                {
                    var queryData = (EntityQueryData*) archetype->MatchingQueryData.Ptr[queryIndex];
                    queryData->InvalidateChunkCache();
                }

                var nextArchetype = archetype->NextChangedArchetype;
                archetype->NextChangedArchetype = null;
                archetype = nextArchetype;
            }

            m_ChunkListChangesTracker.ArchetypeTrackingHead = null;
        }

        internal void IncrementSharedComponentVersions(Archetype* archetype, SharedComponentValues sharedComponentValues)
        {
            for (var i = 0; i < archetype->NumSharedComponents; i++)
                IncrementSharedComponentVersion_Unmanaged(sharedComponentValues[i]);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        public void AssertNoQueuedCompanionCommands()
        {
            var isEmpty = CompanionChangesTracker.Empty;
            CompanionChangesTracker.Reset();
            Assert.IsTrue(isEmpty);
        }

        // Synchronous per-entity hook that queues companion GameObject destruction. It is played back in the
        // same cycle as the structural change, so DestroyEntity destroys the GameObject before returning.
        public void DeallocateCompanionGameObjects(ChunkIndex chunk, int indexInChunk, int batchCount)
        {
            var archetype = GetArchetype(chunk);
            // Cheap flag gate so non-companion destroys skip the type lookup entirely; the flag is set
            // whenever a CompanionLink is present, so this can't skip a real companion.
            if (!archetype->HasCompanionComponents)
                return;
            var linkIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(archetype, CompanionComponentStore.CompanionLinkTypeIndex);
            if (linkIndexInArchetype == -1)
                return;
            var linkPtr = (EntityId*)ChunkDataUtility.GetComponentDataRO(chunk, archetype, indexInChunk, linkIndexInArchetype);
            CompanionChangesTracker.QueueDestroyCompanionGameObjects(linkPtr, batchCount);
        }

    }

    unsafe struct LookupCache
    {
        // This struct caches information about single component type, which is expected to be accessed in several
        // chunks within a given archetype. The type itself is not stored.
        // If the Archetype pointer matches the target archetype, assume the cache is valid. and all fields can be
        // read safely. However, a valid cache may still have IndexInArchetype=-1 (if the archetype does not have the
        // component type). In this case, the remaining fields are undefined and should not be used.
        // If the Archetype pointer does not match the target archetype, the cache is stale and must be updated before
        // any field can be used.
        // No fields should be modified outside of the Update() method.
        // A cache instance should only ever be used within the context of a single component type. Updating the cache
        // to store a different type in the same archetype is not supported.
        [NativeDisableUnsafePtrRestriction]
        public Archetype* Archetype;
        public int        ComponentOffset;
        public ushort     ComponentSizeOf;
        public short      IndexInArchetype;

        // This method will *always* update the cache.
        // It should only be called if it's already been determined that the cache is stale.
        // It is safe to call if the archetype does not contain the type.
        public void Update(Archetype *archetype, TypeIndex typeIndex)
        {
            ChunkDataUtility.GetIndexInTypeArray(archetype, typeIndex, ref IndexInArchetype);
            ComponentOffset = IndexInArchetype == -1 ? 0 : archetype->Offsets[IndexInArchetype];
            ComponentSizeOf = IndexInArchetype == -1 ? (ushort)0 : archetype->SizeOfs[IndexInArchetype];
            Archetype = archetype;
        }
    }
}
