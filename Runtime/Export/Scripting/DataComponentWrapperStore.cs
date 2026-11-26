// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine
{
    // Wrappers are compared by reference, so an id must always get the same wrapper while its owner entity lives.
    // "is object", not "!= null": the Object operator asks native whether the component exists.
    [VisibleToOtherModules]
    internal static class DataComponentWrapperStore
    {
        const int k_PageBits = 14;
        const int k_PageSize = 1 << k_PageBits;
        const int k_PageMask = k_PageSize - 1;
        const int k_RootSize = 1 << (28 - k_PageBits);

        // No data component has type id 0, so 0 marks an empty lane.
        const int k_LanesPerWord = 4;
        const ulong k_LaneLowBits = 0x0001000100010001UL;
        const ulong k_LaneHighBits = 0x8000800080008000UL;

        // Written into every empty lane of a leaf that is being replaced, so that no claim lands after its copy.
        const ulong k_SealedLane = 0xFFFF;

        // Lanes fill in order, so the first empty lane ends a search.
        sealed class Leaf
        {
            public readonly ulong ownerId;
            public long keys;
            public Component wrapper0;
            public Component wrapper1;
            public Component wrapper2;
            public Component wrapper3;
            public readonly long[] moreKeys;
            public readonly Component[] moreWrappers;

            public Leaf(ulong ownerId, int capacity)
            {
                this.ownerId = ownerId;
                if (capacity > k_LanesPerWord)
                {
                    moreKeys = new long[(capacity - k_LanesPerWord) / k_LanesPerWord];
                    moreWrappers = new Component[capacity - k_LanesPerWord];
                }
            }

            public uint version => (uint)(ownerId >> 40);

            public int wordCount => moreKeys == null ? 1 : moreKeys.Length + 1;
        }

        // Kept across play mode changes, so entities that live through them keep their wrappers.
        [NoAutoStaticsCleanup]
        static Leaf[][] s_Pages;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsDataComponentId(EntityId id) => (EntityId.ToULong(id) & EntityId.k_TypeIdMask) != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static EntityId GetOwnerId(EntityId id) => EntityId.FromULong(EntityId.ToULong(id) & ~EntityId.k_TypeIdMask);

        internal static EntityId GetIdOnOwner(EntityId id, EntityId ownerId) => EntityId.FromULong(EntityId.ToULong(ownerId) | (EntityId.ToULong(id) & EntityId.k_TypeIdMask));

        internal static bool TryGet(EntityId id, out Component wrapper)
        {
            wrapper = null;
            uint index = id.Index;
            var root = Volatile.Read(ref s_Pages);
            if (root == null)
                return false;

            var page = Volatile.Read(ref root[index >> k_PageBits]);
            if (page == null)
                return false;

            var leaf = Volatile.Read(ref page[index & k_PageMask]);
            if (leaf == null || leaf.version != id.Version)
                return false;

            int slot = FindSlot(leaf, (ulong)id.TypeId);
            if (slot < 0)
                return false;

            // A claimed slot without a wrapper is a create in progress; GetOrCreate finishes it.
            wrapper = Volatile.Read(ref WrapperAt(leaf, slot));
            return wrapper is object;
        }

        // Does not check that the component exists; the caller does.
        internal static Component GetOrCreate(EntityId id)
        {
            if (!IsDataComponentId(id))
                throw new ArgumentException($"EntityId {id} is not a data component.", nameof(id));

            uint index = id.Index;
            ulong typeId = id.TypeId;
            var page = GetOrAllocatePage(index >> k_PageBits);
            ref Leaf leafRef = ref page[index & k_PageMask];

            while (true)
            {
                var leaf = Volatile.Read(ref leafRef);
                if (leaf == null || IsOlderVersion(leaf.version, id.Version))
                {
                    var created = new Leaf(EntityId.ToULong(GetOwnerId(id)), k_LanesPerWord);
                    var wrapper = new Component(id);
                    created.keys = (long)typeId;
                    created.wrapper0 = wrapper;
                    if (Interlocked.CompareExchange(ref leafRef, created, leaf) == leaf)
                        return wrapper;
                    continue;
                }

                if (leaf.version != id.Version)
                    return null;

                int slot = FindSlot(leaf, typeId);
                if (slot < 0)
                    slot = ClaimSlot(leaf, typeId);
                if (slot >= 0)
                    return FillWrapper(leaf, slot, typeId);

                Grow(ref leafRef, leaf);
            }
        }

        // Drops every wrapper of the owner entity, unless the index already holds a newer entity.
        internal static void Remove(EntityId ownerId)
        {
            uint index = ownerId.Index;
            var root = Volatile.Read(ref s_Pages);
            if (root == null)
                return;

            var page = Volatile.Read(ref root[index >> k_PageBits]);
            if (page == null)
                return;

            ref Leaf leafRef = ref page[index & k_PageMask];
            var leaf = Volatile.Read(ref leafRef);
            if (leaf != null && leaf.version == ownerId.Version)
                Interlocked.CompareExchange(ref leafRef, null, leaf);
        }

        internal static void Clear()
        {
            Volatile.Write(ref s_Pages, null);
        }

        static Leaf[][] GetOrAllocateRoot()
        {
            var root = Volatile.Read(ref s_Pages);
            if (root != null)
                return root;

            var created = new Leaf[k_RootSize][];
            return Interlocked.CompareExchange(ref s_Pages, created, null) ?? created;
        }

        static Leaf[] GetOrAllocatePage(uint pageIndex)
        {
            ref Leaf[] pageRef = ref GetOrAllocateRoot()[pageIndex];
            var page = Volatile.Read(ref pageRef);
            if (page != null)
                return page;

            var created = new Leaf[k_PageSize];
            return Interlocked.CompareExchange(ref pageRef, created, null) ?? created;
        }

        // Versions are 24-bit and wrap, so compare them by their signed distance.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsOlderVersion(uint version, uint than) => (int)((than - version) << 8) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref long KeyWordAt(Leaf leaf, int word)
        {
            if (word == 0)
                return ref leaf.keys;
            return ref leaf.moreKeys[word - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref Component WrapperAt(Leaf leaf, int slot)
        {
            switch (slot)
            {
                case 0: return ref leaf.wrapper0;
                case 1: return ref leaf.wrapper1;
                case 2: return ref leaf.wrapper2;
                case 3: return ref leaf.wrapper3;
                default: return ref leaf.moreWrappers[slot - k_LanesPerWord];
            }
        }

        // A lane above an equal lane can be marked falsely, so only the lowest mark is exact.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong MatchLanes(ulong word, ulong key)
        {
            ulong x = word ^ (key * k_LaneLowBits);
            return (x - k_LaneLowBits) & ~x & k_LaneHighBits;
        }

        // marks must be non-zero.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int LowestLane(ulong marks)
        {
            ulong lowest = marks & (ulong)-(long)marks;
            return (int)(((lowest >> 15) * 0x0000000100020003UL) >> 48);
        }

        static int FindSlot(Leaf leaf, ulong typeId)
        {
            ulong word = (ulong)Volatile.Read(ref leaf.keys);
            ulong matches = MatchLanes(word, typeId);
            if (matches != 0)
                return LowestLane(matches);
            if (MatchLanes(word, 0) != 0 || leaf.moreKeys == null)
                return -1;
            return FindSlotInMoreKeys(leaf, typeId);
        }

        static int FindSlotInMoreKeys(Leaf leaf, ulong typeId)
        {
            var moreKeys = leaf.moreKeys;
            for (int i = 0; i < moreKeys.Length; ++i)
            {
                ulong word = (ulong)Volatile.Read(ref moreKeys[i]);
                ulong matches = MatchLanes(word, typeId);
                if (matches != 0)
                    return (i + 1) * k_LanesPerWord + LowestLane(matches);
                if (MatchLanes(word, 0) != 0)
                    return -1;
            }
            return -1;
        }

        // Returns the lane another thread already claimed for typeId, or -1 when the leaf is full or sealed.
        static int ClaimSlot(Leaf leaf, ulong typeId)
        {
            for (int i = 0; i < leaf.wordCount; ++i)
            {
                ref long keyWord = ref KeyWordAt(leaf, i);
                ulong word = (ulong)Volatile.Read(ref keyWord);
                while (true)
                {
                    ulong matches = MatchLanes(word, typeId);
                    if (matches != 0)
                        return i * k_LanesPerWord + LowestLane(matches);
                    if (MatchLanes(word, k_SealedLane) != 0)
                        return -1;

                    ulong empty = MatchLanes(word, 0);
                    if (empty == 0)
                        break;

                    int lane = LowestLane(empty);
                    ulong claimed = word | (typeId << (lane * 16));
                    ulong seen = (ulong)Interlocked.CompareExchange(ref keyWord, (long)claimed, (long)word);
                    if (seen == word)
                        return i * k_LanesPerWord + lane;
                    word = seen;
                }
            }
            return -1;
        }

        // Any thread can build the wrapper, so a claimed but unfilled slot is filled instead of waited on.
        static Component FillWrapper(Leaf leaf, int slot, ulong typeId)
        {
            ref Component wrapperRef = ref WrapperAt(leaf, slot);
            var existing = Volatile.Read(ref wrapperRef);
            if (existing is object)
                return existing;

            var wrapper = new Component(EntityId.FromULong(leaf.ownerId | (typeId << EntityId.k_TypeIdShift)));
            return Interlocked.CompareExchange(ref wrapperRef, wrapper, null) ?? wrapper;
        }

        // Seal, then fill, then copy, so the copy holds exactly the wrappers that readers of the old leaf see.
        static void Grow(ref Leaf leafRef, Leaf leaf)
        {
            int wordCount = leaf.wordCount;
            for (int i = 0; i < wordCount; ++i)
            {
                ref long keyWord = ref KeyWordAt(leaf, i);
                ulong word = (ulong)Volatile.Read(ref keyWord);
                while (true)
                {
                    // Lanes fill in order, so every lane above the first empty one is empty too and the marks are exact.
                    ulong empty = MatchLanes(word, 0);
                    if (empty == 0)
                        break;
                    ulong sealedWord = word | ((empty >> 15) * k_SealedLane);
                    ulong seen = (ulong)Interlocked.CompareExchange(ref keyWord, (long)sealedWord, (long)word);
                    if (seen == word)
                        break;
                    word = seen;
                }
            }

            var grown = new Leaf(leaf.ownerId, wordCount * k_LanesPerWord * 2);
            int count = 0;
            for (int i = 0; i < wordCount; ++i)
            {
                ulong word = (ulong)Volatile.Read(ref KeyWordAt(leaf, i));
                for (int lane = 0; lane < k_LanesPerWord; ++lane)
                {
                    ulong typeId = (word >> (lane * 16)) & 0xFFFF;
                    if (typeId == k_SealedLane)
                        break;
                    KeyWordAt(grown, count / k_LanesPerWord) |= (long)(typeId << (count % k_LanesPerWord * 16));
                    WrapperAt(grown, count) = FillWrapper(leaf, i * k_LanesPerWord + lane, typeId);
                    ++count;
                }
            }

            Interlocked.CompareExchange(ref leafRef, grown, leaf);
        }
    }
}
