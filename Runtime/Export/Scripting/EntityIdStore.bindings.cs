// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Scripting.LifecycleManagement;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Bindings for the layout/info exports added to Runtime/BaseClasses/EntityIdStore.{h,cpp}
    // and the GCHandle-offset helper in BaseObject.{h,cpp}. Kept private to this file so the
    // EntityIdStore type below is the only consumer.
    [NativeHeader("Runtime/BaseClasses/EntityIdStore.h")]
    [NativeHeader("Runtime/BaseClasses/BaseObject.h")]
    internal static class EntityIdStoreBindings
    {
        [NativeMethod(Name = "Object::GetOffsetOfGCHandleMember", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern int GetOffsetOfGCHandleInCPlusPlusObject();

        [NativeMethod(Name = "GetEntityIdAllocatorStore", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetEntityIdAllocatorStore();

        [NativeMethod(Name = "GetEntityIdStoreBlockShift", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetEntityIdStoreBlockShift();

        [NativeMethod(Name = "GetEntityIdStoreBlockMask", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetEntityIdStoreBlockMask();

        [NativeMethod(Name = "GetEntityIdStoreBlockCount", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetEntityIdStoreBlockCount();

        [NativeMethod(Name = "GetEntityIdStoreWordsPerBlock", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetEntityIdStoreWordsPerBlock();

        [NativeMethod(Name = "GetEntityIdStoreEntityCount", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetEntityIdStoreEntityCount();

        // null on page-table path; null on non-editor builds for reserved.
        [NativeMethod(Name = "GetEntityIdStoreAllocatedBits", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetEntityIdStoreAllocatedBits();

        [NativeMethod(Name = "GetEntityIdStoreReservedBits", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetEntityIdStoreReservedBits();

        // null on page-table and on-demand-commit paths.
        [NativeMethod(Name = "GetEntityIdStoreBlockCommittedTable", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetEntityIdStoreBlockCommittedTable();

        // Address of the committed-index bound the fast-path reader gates slot
        // reads on (native baselib::atomic<UInt32>). null on the page-table path.
        [NativeMethod(Name = "GetEntityIdStoreCommittedIndexBoundAddress", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetEntityIdStoreCommittedIndexBoundAddress();

        [NativeMethod(Name = "EntityIdStorePlatformSupportsVirtualMemory", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern bool EntityIdStorePlatformSupportsVirtualMemory();

        // Fallbacks into the native pool for whatever the direct magazine
        // paths cannot serve: Burst callers, refills, reserved and stale ids,
        // and teardown-scale batches.
        [NativeMethod(Name = "EntityIdStore_AllocateForManaged", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void AllocateForManaged(void* outIds, int count);

        [NativeMethod(Name = "EntityIdStore_ReleaseForManaged", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void ReleaseForManaged(void* ids, int count);

        // The calling thread's native magazine cache, for the direct managed
        // fast paths. The layout getters below address its fields.
        [NativeMethod(Name = "EntityIdStore_GetThreadCacheForManaged", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetThreadCacheForManaged();

        [NativeMethod(Name = "EntityIdStore_GetMagazineCapacity", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetMagazineCapacity();

        [NativeMethod(Name = "EntityIdStore_GetMagazineCountOffset", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetMagazineCountOffset();

        [NativeMethod(Name = "EntityIdStore_GetMagazineIdsOffset", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetMagazineIdsOffset();

        [NativeMethod(Name = "EntityIdStore_GetCacheActiveOffset", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetCacheActiveOffset();

        [NativeMethod(Name = "EntityIdStore_GetCacheBackupOffset", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern uint GetCacheBackupOffset();
    }

    // Managed view of the native EntityIdStore. The layout must stay in sync
    // with Runtime/BaseClasses/EntityIdStore.{h,cpp}: EntitySlot is 16 bytes
    // (ulong versionAndChunk [chunkIndex:32|indexInChunk:8|version:24] plus
    // IntPtr nativeObjectPtr); EntityId packs [Version:24|TypeId:12|Index:28].
    // The storage mode is decided natively and queried once at init because
    // C# has no 64-bit compile-time define.
    internal unsafe partial class EntityIdStore
    {
        // Mirrors native EntitySlot in Runtime/BaseClasses/EntityIdStore.h.
        // Reading versionAndChunk and nativeObjectPtr as plain values is safe on
        // x86/arm64: the native side uses baselib::atomic only for memory ordering;
        // it has the same layout as the underlying type.
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        internal struct EntitySlot
        {
            public ulong versionAndChunk;
            public IntPtr nativeObjectPtr;
        }

        internal const ulong k_SlotVersionMask = (1UL << 24) - 1;

        // Mirror C++ k_SlotIndexInChunkShift / k_SlotChunkIndexShift.
        internal const int k_IndexInChunkShift = 24;
        internal const int k_ChunkIndexShift   = 32;

        // Mirror C++ SlotGetVersion / SlotPackVersionAndChunk / SlotSetVersion.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint  SlotGetVersion(ulong vac)      => (uint)(vac & k_SlotVersionMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int   SlotGetIndexInChunk(ulong vac) => (int)((vac >> k_IndexInChunkShift) & 0xFFUL);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int   SlotGetChunkIndex(ulong vac)   => (int)(uint)(vac >> k_ChunkIndexShift);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong SlotPackVersionAndChunk(uint version, byte indexInChunk, uint chunkIndex)
            => ((ulong)chunkIndex   << k_ChunkIndexShift)
             | ((ulong)indexInChunk << k_IndexInChunkShift)
             | (version & k_SlotVersionMask);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong SlotSetVersion(ulong vac, uint newVersion)
            => (vac & ~k_SlotVersionMask) | (newVersion & k_SlotVersionMask);

        // Mirrors C++ k_BlockBusy.
        internal const int k_BlockBusy = -1;

        // Mirror C++ k_WordShift / k_WordMask.
        internal const int  k_WordShift = 6;
        internal const uint k_WordMask  = 63;

        // Layout fields that Burst-compiled code must be able to read are stored in a
        // SharedStatic so the Burst JIT never needs to call (or analyse) the class
        // initializer, which contains extern calls that Burst cannot compile.
        // Populated once by Initialize(), which is called eagerly via [OnCodeLoaded]
        // so Burst-compiled callers always see a valid context (Burst cannot execute
        // the P/Invoke calls inside Initialize() itself).
        internal struct ContextData
        {
            internal struct BurstIdentifier {}

            // Storage-mode selector; the JIT folds branches on this after Initialize().
            public bool PlatformSupportsVirtualMemory;
            // Block geometry.
            public int   BlockShift;
            public uint  BlockMask;
            // Allocator tables, used by the integrity checks only; allocation
            // and release forward to native.
            public int*  EntityCount;    // baselib::atomic<int>[], same layout as int[]
            public uint  BlockCount;
            public uint  WordsPerBlock;
            public ulong* AllocatedBits; // null on page-table path
            public ulong* ReservedBits;  // null on page-table path and in player builds
            public byte*  BlockCommitted;// null on page-table path
            public uint*  CommittedIndexBound; // read-side gate: one past the committed index prefix; null on page-table path
            // Magazine-cache layout, exported so the direct fast paths can
            // address the native cache and magazine fields.
            public int MagCapacity;
            public int MagCountOffset;
            public int MagIdsOffset;
            public int CacheActiveOffset;
            public int CacheBackupOffset;
            // Raw store pointer: EntitySlot* (VM path) or Block** (page-table path).
            public void*  NativeStore;
            // Zeroed sentinel slot: version 0 never matches any live entity.
            public EntitySlot NullSlot;
            // Byte offset of the GCHandle member inside a C++ Object.
            public int OffsetOfGCHandleInObject;
            // Set to true by Initialize() so subsequent calls are no-ops.
            public bool IsInitialized;
        }

        internal static readonly SharedStatic<ContextData> s_Context =
            SharedStatic<ContextData>.GetOrCreate<ContextData.BurstIdentifier>();

        // Populates the SharedStatic layout from native once; subsequent calls
        // are no-ops. [OnCodeLoaded] runs this at domain load, before any
        // Burst-compiled caller can reach AllocateEntityIds; Burst cannot
        // execute the P/Invoke calls here, so eager managed-side
        // initialization is required.
        [Unity.Scripting.LifecycleManagement.OnCodeLoaded]
        internal static void Initialize()
        {
            ref ContextData ctx = ref s_Context.Data;
            if (ctx.IsInitialized) return;
            ctx.PlatformSupportsVirtualMemory = EntityIdStoreBindings.EntityIdStorePlatformSupportsVirtualMemory();
            ctx.BlockShift    = (int)EntityIdStoreBindings.GetEntityIdStoreBlockShift();
            ctx.BlockMask     = EntityIdStoreBindings.GetEntityIdStoreBlockMask();
            ctx.EntityCount   = (int*)EntityIdStoreBindings.GetEntityIdStoreEntityCount();
            ctx.BlockCount    = EntityIdStoreBindings.GetEntityIdStoreBlockCount();
            ctx.WordsPerBlock = EntityIdStoreBindings.GetEntityIdStoreWordsPerBlock();
            ctx.AllocatedBits = (ulong*)EntityIdStoreBindings.GetEntityIdStoreAllocatedBits();
            ctx.ReservedBits  = (ulong*)EntityIdStoreBindings.GetEntityIdStoreReservedBits();
            ctx.BlockCommitted= (byte*)EntityIdStoreBindings.GetEntityIdStoreBlockCommittedTable();
            ctx.CommittedIndexBound = (uint*)EntityIdStoreBindings.GetEntityIdStoreCommittedIndexBoundAddress();
            ctx.MagCapacity       = (int)EntityIdStoreBindings.GetMagazineCapacity();
            ctx.MagCountOffset    = (int)EntityIdStoreBindings.GetMagazineCountOffset();
            ctx.MagIdsOffset      = (int)EntityIdStoreBindings.GetMagazineIdsOffset();
            ctx.CacheActiveOffset = (int)EntityIdStoreBindings.GetCacheActiveOffset();
            ctx.CacheBackupOffset = (int)EntityIdStoreBindings.GetCacheBackupOffset();
            ctx.NativeStore              = EntityIdStoreBindings.GetEntityIdAllocatorStore();
            ctx.NullSlot                 = default;
            ctx.OffsetOfGCHandleInObject = EntityIdStoreBindings.GetOffsetOfGCHandleInCPlusPlusObject();
            // The Block struct below hardcodes the native block geometry; a
            // mismatch would misplace the bitmaps inside the slot array.
            Assert.AreEqual(1u << ctx.BlockShift, (uint)k_NativeBlockSlots);
            Assert.AreEqual(ctx.WordsPerBlock, (uint)k_NativeBlockWords);
            ctx.IsInitialized = true;
        }



        // ----------------------------------------------------------------------
        // Native slot lookup
        // ----------------------------------------------------------------------

        // Slot for the entity index, or a zeroed sentinel for uncommitted
        // pages (version 0 never matches, so the version check is the single
        // validation step). Mirrors native EntityIdStore_GetSlot.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ref EntitySlot GetSlot(uint entityIndex)
        {
            ref ContextData ctx = ref s_Context.Data;
            if (ctx.PlatformSupportsVirtualMemory)
            {
                // Committed blocks form a contiguous index prefix, so a single
                // bound check replaces the per-block committed-flag lookup.
                // Volatile.Read is the acquire that pairs with the native
                // committer's release stores, as in EntityIdStore_GetSlot.
                if (entityIndex >= Volatile.Read(ref *ctx.CommittedIndexBound))
                    return ref ctx.NullSlot;
                return ref ((EntitySlot*)ctx.NativeStore)[entityIndex];
            }
            else
            {
                // Volatile.Read pairs with native CommitBlock's release publish
                // of the block pointer, so a non-null entry implies the block's
                // zeroed contents are visible.
                EntitySlot* slots = (EntitySlot*)Volatile.Read(
                    ref ((IntPtr*)ctx.NativeStore)[entityIndex >> ctx.BlockShift]);
                if (slots == null)
                    return ref ctx.NullSlot;
                return ref slots[entityIndex & ctx.BlockMask];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool BlockIsCommitted(uint blockIndex)
        {
            ref ContextData ctx = ref s_Context.Data;
            if (ctx.PlatformSupportsVirtualMemory)
                // BlockCommitted is null on the VM on-demand-commit path; null means all blocks
                // are committed on access (the page fault handler commits them).
                return ctx.BlockCommitted == null || Volatile.Read(ref ctx.BlockCommitted[blockIndex]) != 0;
            return Volatile.Read(ref ((IntPtr*)ctx.NativeStore)[blockIndex]) != IntPtr.Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong* BlockAllocated(uint blockIndex)
        {
            ref ContextData ctx = ref s_Context.Data;
            if (ctx.PlatformSupportsVirtualMemory)
                return ctx.AllocatedBits + blockIndex * ctx.WordsPerBlock;
            return ((Block**)ctx.NativeStore)[blockIndex]->allocated;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong* BlockReserved(uint blockIndex)
        {
            ref ContextData ctx = ref s_Context.Data;
            if (ctx.PlatformSupportsVirtualMemory)
                return ctx.ReservedBits + blockIndex * ctx.WordsPerBlock;
            return ((Block**)ctx.NativeStore)[blockIndex]->reserved;
        }

        // Mirrors C++ struct Block in EntityIdStore.cpp (page-table path only).
        // slots[] is at offset 0 so (EntitySlot*)blockPtr == &block->slots[0].
        // Block geometry is compile-time on both sides (1024 slots, 16 bitmap
        // words); Initialize() cross-checks these against the native exports.
        internal const int k_NativeBlockSlots = 1024; // C++ gEntitiesInBlock
        internal const int k_NativeBlockWords = 16;   // C++ gWordsPerBlock
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct Block
        {
            fixed byte slotsRaw[k_NativeBlockSlots * 16];        // EntitySlot slots[1024] at offset 0
            public fixed ulong allocated[k_NativeBlockWords];     // UInt64 allocated[gWordsPerBlock]
            public fixed ulong reserved[k_NativeBlockWords];
        }

        // ----------------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------------

        // Mirrors C++ EntityExists; version 0 never belongs to a live entity.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Exists(EntityId entity)
        {
            if (entity.Version == 0) return false;
            ref EntitySlot slot = ref GetSlot(entity.Index);
            // Plain read (native uses relaxed): GetSlot's acquire read of the
            // bound / block pointer keeps it ordered, and version bits sit in
            // the low 32-bit half so a torn read still yields a published version.
            return SlotGetVersion(slot.versionAndChunk) == entity.Version;
        }

        // Overwrites the version stored in an entity's slot. Version 0 is the
        // uninitialized-slot sentinel and must never be written to a live slot.
        internal static void SetEntityVersion(EntityId entity, int version)
        {
            uint maskedVersion = (uint)version & (uint)k_SlotVersionMask;
            Assert.AreNotEqual(0u, maskedVersion);
            ref EntitySlot slot = ref GetSlot(entity.Index);
            // GetSlot returns the shared sentinel for uncommitted indices, and a
            // nonzero version written into it would make every stale id on an
            // uncommitted page look live.
            if (UnsafeUtility.AddressOf(ref slot) == UnsafeUtility.AddressOf(ref s_Context.Data.NullSlot))
            {
                Assert.IsTrue(false, "SetEntityVersion: entity index is out of bounds or not committed.");
                return;
            }
            ulong vac = Volatile.Read(ref slot.versionAndChunk);
            Volatile.Write(ref slot.versionAndChunk, SlotSetVersion(vac, maskedVersion));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void* GetNativeObject(EntityId entity)
        {
            ref EntitySlot slot = ref GetSlot(entity.Index);

            // Mirrors native GetNativePtr: versions only move forward, so a
            // version matching after the (acquire) pointer load also matched
            // before it.
            IntPtr ptr = Volatile.Read(ref slot.nativeObjectPtr);
            // Plain read (native uses relaxed): the acquire read above keeps it
            // ordered. Version bits sit in the low 32-bit half, so a torn read
            // on 32-bit platforms still yields a published version.
            ulong vac = slot.versionAndChunk;

            if ((uint)(vac & k_SlotVersionMask) != entity.Version)
                return null;

            return (void*)ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetManagedObject<T>(EntityId entity) where T : UnityEngine.Object
        {
            void* objectPtr = GetNativeObject(entity);
            if (objectPtr == null)
                return null;

            GCHandle handle = *(GCHandle*)((byte*)objectPtr + s_Context.Data.OffsetOfGCHandleInObject);
            // Resident natively but not yet wrapped (e.g. a baked / deserialized ref on
            // first access): treat as a miss so the caller falls back to native resolution,
            // which materializes the wrapper. Reading Target on an empty handle would throw.
            if (!handle.IsAllocated)
                return null;
            return UnsafeUtility.As<T>(handle.Target);
        }

        // ----------------------------------------------------------------------
        // Integrity check; mirrors C++ IntegrityCheck in EntityIdStore.cpp.

        // Counts allocated entity IDs, skipping blocks that are currently locked.
        // Thread-unsafe by design; call only from single-threaded diagnostic contexts.
        internal static int DebugOnlyThreadUnsafeEntityCount()
        {
            int count = 0;
            for (uint i = 0; i < s_Context.Data.BlockCount; i++)
            {
                int v = s_Context.Data.EntityCount[i];
                if (v != k_BlockBusy) count += v;
            }
            return count;
        }

        // Verifies that the allocated bitmap matches the slot parity invariant
        // (odd version = alive) for every entity in a block.
        internal static void IntegrityCheck(int blockIndex)
        {
            if (!BlockIsCommitted((uint)blockIndex))
            {
                Assert.AreEqual(0, s_Context.Data.EntityCount[blockIndex]);
                return;
            }

            ulong* allocated     = BlockAllocated((uint)blockIndex);
            ulong* reserved      = BlockReserved((uint)blockIndex);
            uint entitiesInBlock = s_Context.Data.BlockMask + 1;
            uint baseIndex       = (uint)blockIndex * entitiesInBlock;

            for (uint i = 0; i < entitiesInBlock; i++)
            {
                uint entityIndex = baseIndex + i;
                if (entityIndex >= (1u << 28)) break; // 28 = EntityId.kIndexBits

                var aliveA = (allocated[i >> k_WordShift] >> (int)(i & k_WordMask)) & 1UL;
                ref var slot = ref GetSlot(entityIndex);
                var aliveB = (ulong)(SlotGetVersion(Volatile.Read(ref slot.versionAndChunk)) & 1u);

                var isReserved = (reserved[i >> k_WordShift] >> (int)(i & k_WordMask)) & 1UL;
                if (isReserved != 0) continue;
                Assert.AreEqual(aliveA, aliveB);
            }
        }

        internal static void IntegrityCheck()
        {
            for (int i = 0; i < (int)s_Context.Data.BlockCount; i++)
                IntegrityCheck(i);
        }

        // ----------------------------------------------------------------------
        // Allocation. Managed fast paths operate the calling thread's native
        // magazine cache directly: the pointer comes from one native call and
        // lives in thread-local storage, which no other thread can read and
        // which dies with the thread. Native is crossed only where the cache
        // cannot serve: magazine refills and depot exchanges, reserved and
        // stale ids, teardown-scale batches, and Burst-compiled callers, for
        // whom the thread-static is unreachable and t_ThreadCache reads null.
        // ----------------------------------------------------------------------

        // Valid across code reloads: the native store and the thread survive,
        // so the cached pointer stays correct; a runtime that resets
        // thread-statics anyway just causes a re-fetch.
        [NoAutoStaticsCleanup]
        [ThreadStatic] static IntPtr t_ThreadCache;

        // Null under Burst (the helper is discarded) and before the thread's
        // first use; both fall back to the native calls.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte* GetThreadCache()
        {
            IntPtr cache = IntPtr.Zero;
            GetThreadCacheManaged(ref cache);
            return (byte*)cache;
        }

        [BurstDiscard]
        static void GetThreadCacheManaged(ref IntPtr cache)
        {
            IntPtr c = t_ThreadCache;
            if (c == IntPtr.Zero)
            {
                unsafe { c = (IntPtr)EntityIdStoreBindings.GetThreadCacheForManaged(); }
                t_ThreadCache = c;
            }
            cache = c;
        }

        // Entry point: served from this thread's magazines, with the remainder
        // crossing into native. Chunk placement is stamped at hand-out time on
        // slots the allocation already owns, so every allocation takes the
        // same path regardless of chunk bits.
        internal static void AllocateEntityIds(EntityId* outIds, int count,
            uint chunkBits = 0, byte firstIndexInChunk = 0)
        {
            if (count <= 0) return;

            ref ContextData ctx = ref s_Context.Data;
            int produced = 0;
            byte* cache = count <= 2 * ctx.MagCapacity ? GetThreadCache() : null;
            while (cache != null && produced < count)
            {
                byte* mag = *(byte**)(cache + ctx.CacheActiveOffset);
                int magCount = *(int*)(mag + ctx.MagCountOffset);
                if (magCount == 0)
                {
                    byte* backup = *(byte**)(cache + ctx.CacheBackupOffset);
                    if (*(int*)(backup + ctx.MagCountOffset) == 0)
                        break; // refill crosses into native
                    *(byte**)(cache + ctx.CacheBackupOffset) = mag;
                    *(byte**)(cache + ctx.CacheActiveOffset) = backup;
                    continue;
                }
                int take = count - produced;
                if (take > magCount) take = magCount;
                magCount -= take;
                *(int*)(mag + ctx.MagCountOffset) = magCount;
                UnsafeUtility.MemCpy(outIds + produced,
                    (EntityId*)(mag + ctx.MagIdsOffset) + magCount, (long)take * sizeof(EntityId));
                produced += take;
            }
            if (produced < count)
                EntityIdStoreBindings.AllocateForManaged(outIds + produced, count - produced);

            if (chunkBits != 0 || firstIndexInChunk != 0)
                StampChunk(outIds, count, chunkBits, firstIndexInChunk);
        }

        // Writes DOTS chunk placement into freshly allocated slots. The
        // allocation owns these slots, so plain ordering is enough; concurrent
        // readers only validate the version, which does not change.
        static void StampChunk(EntityId* ids, int count, uint chunkBits, byte firstIndexInChunk)
        {
            byte indexInChunk = firstIndexInChunk;
            for (int i = 0; i < count; ++i)
            {
                ref EntitySlot slot = ref GetSlot(ids[i].Index);
                Volatile.Write(ref slot.versionAndChunk,
                    SlotPackVersionAndChunk(ids[i].Version, indexInChunk++, chunkBits));
            }
        }

        // Recycles one id in place, mirroring the native TryRecycleIdInPlace:
        // no lock and no shared-state mutation, just the owned slot moving
        // from live to cached (version stepped +2, pointer nulled, chunk bits
        // cleared). False routes the id to native: stale ids (that path owns
        // the asserts) and editor-reserved ids (their release parks the slot).
        static bool TryRecycleIdInPlace(ref ContextData ctx, EntityId id, out EntityId recycled)
        {
            recycled = default;
            uint entityIndex = id.Index;
            uint blockIndex = entityIndex >> ctx.BlockShift;

            if (!BlockIsCommitted(blockIndex))
                return false;

            uint indexInBlock = entityIndex & ctx.BlockMask;
            ulong mask = 1UL << (int)(indexInBlock & k_WordMask);
            uint wordIdx = indexInBlock >> k_WordShift;

            // Unlocked probes are safe: only the id's owner may release it, so
            // this slot's bit and version cannot change underneath us.
            if ((BlockAllocated(blockIndex)[wordIdx] & mask) == 0)
                return false;

            if ((BlockReserved(blockIndex)[wordIdx] & mask) != 0)
                return false;

            ref EntitySlot slot = ref GetSlot(entityIndex);
            uint version = SlotGetVersion(slot.versionAndChunk);
            if (version != id.Version)
                return false;

            // Store order matches the native release: pointer first, then the
            // version with release, so a reader that still version-matches can
            // only have loaded the pointer from before this release.
            slot.nativeObjectPtr = IntPtr.Zero;
            uint newVersion = (version + 2) & (uint)k_SlotVersionMask;
            Volatile.Write(ref slot.versionAndChunk, SlotPackVersionAndChunk(newVersion, 0, 0));

            ulong rawId = ((ulong)newVersion << 40) | (entityIndex & 0x0FFFFFFFuL);
            recycled = UnsafeUtility.As<ulong, EntityId>(ref rawId);
            return true;
        }

        // Releases through this thread's magazines; the same-thread LIFO makes
        // a free followed by an allocate return the id just freed. Reserved,
        // stale, and teardown-scale releases cross into native.
        internal static void ReleaseEntityIds(EntityId* ids, int count)
        {
            if (count <= 0) return;

            ref ContextData ctx = ref s_Context.Data;
            byte* cache = count <= 2 * ctx.MagCapacity ? GetThreadCache() : null;
            if (cache == null)
            {
                EntityIdStoreBindings.ReleaseForManaged(ids, count);
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                if (ids[i] == EntityId.None)
                    continue;
                EntityId recycled;
                if (!TryRecycleIdInPlace(ref ctx, ids[i], out recycled))
                {
                    EntityIdStoreBindings.ReleaseForManaged(ids + i, 1);
                    continue;
                }
                byte* mag = *(byte**)(cache + ctx.CacheActiveOffset);
                int magCount = *(int*)(mag + ctx.MagCountOffset);
                if (magCount == ctx.MagCapacity)
                {
                    byte* backup = *(byte**)(cache + ctx.CacheBackupOffset);
                    if (*(int*)(backup + ctx.MagCountOffset) < ctx.MagCapacity)
                    {
                        *(byte**)(cache + ctx.CacheBackupOffset) = mag;
                        *(byte**)(cache + ctx.CacheActiveOffset) = backup;
                        mag = backup;
                        magCount = *(int*)(mag + ctx.MagCountOffset);
                    }
                    else
                    {
                        // The depot exchange crosses into native. The recycled
                        // id is a valid cached id and releasable as-is; native
                        // recycles it once more onto the fresh active magazine.
                        EntityIdStoreBindings.ReleaseForManaged(&recycled, 1);
                        continue;
                    }
                }
                ((EntityId*)(mag + ctx.MagIdsOffset))[magCount] = recycled;
                *(int*)(mag + ctx.MagCountOffset) = magCount + 1;
            }
        }
    }
}
