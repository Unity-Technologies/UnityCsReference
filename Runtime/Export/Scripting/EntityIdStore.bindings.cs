// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
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

        [NativeMethod(Name = "GetEntityIdStoreBlockCommittedTable", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern unsafe void* GetEntityIdStoreBlockCommittedTable();

        [NativeMethod(Name = "EntityIdStorePlatformSupportsVirtualMemory", IsFreeFunction = true, IsThreadSafe = true)]
        public static extern bool EntityIdStorePlatformSupportsVirtualMemory();
    }

    // Managed view of the native EntityIdStore.
    //
    // Layout MUST stay in sync with Runtime/BaseClasses/EntityIdStore.cpp:
    //   - EntitySlot: 16 bytes, ulong versionAndChunk + IntPtr nativeObjectPtr.
    //   - versionAndChunk packing: [chunkIndex:32 | indexInChunk:8 | version:24].
    //   - EntityId packing: [Version:24 | TypeId:12 | Index:28]. See EntityID.h.
    //
    // Storage mode is decided natively and queried once at type init through
    // s_PlatformSupportsVirtualMemory, matching the native side. It cannot be a
    // C# compile-time #if: C# has no 64-bit define and the native
    // PLATFORM_SUPPORTS_ENTITYID_VIRTUAL_MEMORY define is forced off on 32-bit.
    // Either:
    //   Virtual memory mode: a flat EntitySlot array indexed directly by entity index.
    //   Page table mode:     a Block** indexed by (blockIndex, slotInBlock). slots is
    //                        the first member of each Block, so a block pointer is also
    //                        &slots[0] (see GetSlot).
    // The block geometry and committed-table base are queried once via
    // EntityIdStoreBindings and cached in static readonly fields. Because
    // s_PlatformSupportsVirtualMemory is also static readonly, the JIT folds the
    // mode branch in GetSlot and the unused path drops out.
    internal unsafe class EntityIdStore
    {
        // Mirrors native EntitySlot in Runtime/BaseClasses/EntityIdStore.cpp.
        // Reading versionAndChunk and nativeObjectPtr as plain values is safe on
        // x86/arm64: the native side uses baselib::atomic only for memory ordering;
        // it has the same layout as the underlying type.
        [StructLayout(LayoutKind.Sequential, Size = 16)]
        struct EntitySlot
        {
            public ulong versionAndChunk;
            public IntPtr nativeObjectPtr;
        }

        // Version is the LOW 24 bits of versionAndChunk (mask-only extraction).
        const ulong k_SlotVersionMask = (1UL << 24) - 1; // 0x00FFFFFF

        // Static layout info — read once from native at type init.
        // JIT will treat these as constants after the cctor runs.
        static readonly void* m_NativeStore         = EntityIdStoreBindings.GetEntityIdAllocatorStore();
        static readonly int  OffsetOfGCHandleInCPlusPlusObject = EntityIdStoreBindings.GetOffsetOfGCHandleInCPlusPlusObject();
        static readonly int  s_BlockShift           = (int)EntityIdStoreBindings.GetEntityIdStoreBlockShift();

        // Storage-mode selector and the per-mode layout info. Both modes' fields
        // are populated regardless of which mode is active; the inactive one is
        // simply unused (s_BlockMask is 0 / s_BlockCommitted is null on the path
        // that doesn't read it). GetSlot branches on s_PlatformSupportsVirtualMemory.
        static readonly bool s_PlatformSupportsVirtualMemory = EntityIdStoreBindings.EntityIdStorePlatformSupportsVirtualMemory();
        static readonly int  s_BlockMask            = (int)EntityIdStoreBindings.GetEntityIdStoreBlockMask();
        static readonly byte* s_BlockCommitted      = (byte*)EntityIdStoreBindings.GetEntityIdStoreBlockCommittedTable();



        // Mirrors native gNullSlot in EntityIdStore.cpp: a single zeroed slot
        // that GetSlot returns when an entity index resolves to an uncommitted
        // page. Version 0 never matches a live entity, so callers can do a
        // single version compare against the returned slot — no separate null
        // branch — and stale / uncommitted indices fall through naturally.
        static EntitySlot s_NullSlot;


        // ----------------------------------------------------------------------
        // Native slot lookup
        // ----------------------------------------------------------------------

        // Returns a reference to the slot for the given entity index, or to a
        // process-wide zeroed sentinel if the slot is on an uncommitted page.
        // Mirrors native EntitySlot& GetSlot(UInt32) in EntityIdStore.cpp; the
        // sentinel keeps the call sites branch-free and lets the version check
        // act as the single validation step.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref EntitySlot GetSlot(uint entityIndex)
        {
            // s_PlatformSupportsVirtualMemory is static readonly, so the JIT
            // folds this branch and drops the unused path after the cctor runs.
            if (s_PlatformSupportsVirtualMemory)
            {
                uint blockIndex = entityIndex >> s_BlockShift;
                if (Volatile.Read(ref s_BlockCommitted[blockIndex]) == 0)
                    return ref s_NullSlot;
                return ref ((EntitySlot*)m_NativeStore)[entityIndex];
            }
            else
            {
                EntitySlot** blockTable = (EntitySlot**)m_NativeStore;
                EntitySlot* slots = blockTable[entityIndex >> s_BlockShift];
                if (slots == null)
                    return ref s_NullSlot;
                return ref slots[entityIndex & (uint)s_BlockMask];
            }
        }

        // ----------------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------------

        // Pure C# existence check: reads the slot through the published native
        // store pointer and verifies the version matches. No managed↔native crossing.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Exists(EntityId entity)
        {
            ref EntitySlot slot = ref GetSlot(entity.Index);
            ulong versionAndChunk = Volatile.Read(ref slot.versionAndChunk);
            return (uint)(versionAndChunk & k_SlotVersionMask) == entity.Version;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void* GetNativeObject(EntityId entity)
        {
            ref EntitySlot slot = ref GetSlot(entity.Index);
            uint expectedVersion = entity.Version;

            // Version seqlock matching native GetNativePtr. C# has no acquire
            // fence or relaxed atomic load, so all three are acquire loads
            // (Volatile.Read) instead of native's acquire/relaxed mix; the extra
            // ordering is free on x86 and a cheap ldar on ARM64, and far lighter
            // than the full StoreLoad fence Thread.MemoryBarrier() would emit.
            ulong v1 = Volatile.Read(ref slot.versionAndChunk);
            IntPtr ptr = Volatile.Read(ref slot.nativeObjectPtr);
            ulong v2 = Volatile.Read(ref slot.versionAndChunk);

            if ((uint)(v1 & k_SlotVersionMask) != expectedVersion ||
                (uint)(v2 & k_SlotVersionMask) != expectedVersion)
                return null;

            return (void*)ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetManagedObject<T>(EntityId entity) where T : UnityEngine.Object
        {
            void* objectPtr = GetNativeObject(entity);
            if (objectPtr == null)
                return null;

            GCHandle handle = *(GCHandle*)((byte*)objectPtr + OffsetOfGCHandleInCPlusPlusObject);
            // Resident natively but not yet wrapped (e.g. a baked / deserialized ref on
            // first access): treat as a miss so the caller falls back to native resolution,
            // which materializes the wrapper. Reading Target on an empty handle would throw.
            if (!handle.IsAllocated)
                return null;
            return UnsafeUtility.As<T>(handle.Target);
        }
    }
}
