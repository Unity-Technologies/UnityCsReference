// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Text;
using UnityEngine.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.LowLevel;
using Unity.Mathematics;
using Assert = UnityEngine.Assertions.AssertCore;

namespace Unity.Entities
{
    sealed class EntityNameStorageDebugView
    {
        public FixedString64Bytes[] Table
        {
            get
            {
                var table = new FixedString64Bytes[EntityNameStorage.Entries];
                for (var i = 0; i < EntityNameStorage.Entries; ++i)
                    EntityNameStorage.GetFixedString(i, ref table[i]);
                return table;
            }
        }
    }

    /// <summary>
    /// Can hold <see cref="kMaxEntries"/> (up to <see cref="kMaxChars"/>) entity names.
    /// Duplicate entries (with identical hashes) share the same location (<see cref="GetIndexFromHashAndFixedString"/>).
    /// Once added, a name cannot be removed.
    /// Will throw if store is full.
    /// </summary>
    [DebuggerTypeProxy(typeof(EntityNameStorageDebugView))]
    [GenerateTestsForBurstCompatibility]
    internal struct EntityNameStorage
    {
        internal struct Entry
        {
            public int offset;
            public int length;
        }

        internal struct State
        {
            public byte initialized;
            internal byte hasLoggedError;
            public UnsafeList<byte> buffer; // all the UTF-8 encoded bytes in one place
            public UnsafeList<Entry> entry; // one offset for each text in "buffer"
            public UnsafeParallelMultiHashMap<int, int> hash; // from string hash to table entry
            public int chars; // bytes in buffer allocated so far
            public int entries; // number of strings allocated so far
            public FixedString512Bytes kMaxEntriesMsg;
            public UnsafeList<IntPtr> namePages; // name index per entity index, in pages allocated on first use
        }
        internal static readonly SharedStatic<State> s_State = SharedStatic<State>.GetOrCreate<EntityNameStorage>();


        internal const int kMaxEntries = 16 << 10;
        internal const int kMaxChars = kMaxEntries * 64;
        internal const int kErrorExceedMaxEntryCapacity = -1;

        // Names are addressed by Entity.Index, the same way they are looked up. Entity.Index spans
        // 28 bits, too much for a flat array, so the space is paged and a page is allocated the
        // first time a name lands in it. The split is 64KB of page pointers, 128KB per page.
        const int kEntityIndexBits = Entity.kIndexBits;
        const int kIndicesPerPageShift = 15;
        const int kIndicesPerPage = 1 << kIndicesPerPageShift;
        const int kIndexInPageMask = kIndicesPerPage - 1;
        const int kNamePageCount = 1 << (kEntityIndexBits - kIndicesPerPageShift);



        public const int kEntityNameMaxLengthBytes = FixedString64Bytes.utf8MaxLengthInBytes;

        /// <summary>
        /// Returns the number of stored entries (not characters). I.e. Length or Count.
        /// </summary>
        public static int Entries => s_State.Data.entries;

        public static unsafe void Initialize()
        {
            if (s_State.Data.initialized != 0)
                return;

            s_State.Data.buffer = new UnsafeList<byte>(kMaxChars, Allocator.Persistent);
            s_State.Data.buffer.Length = s_State.Data.buffer.Capacity;
            s_State.Data.entry = new UnsafeList<Entry>(kMaxEntries, Allocator.Persistent);
            s_State.Data.entry.Length = s_State.Data.entry.Capacity;
            s_State.Data.hash = new UnsafeParallelMultiHashMap<int, int>(kMaxEntries, Allocator.Persistent);
            s_State.Data.namePages = new UnsafeList<IntPtr>(kNamePageCount, Allocator.Persistent);
            s_State.Data.namePages.Length = kNamePageCount;
            UnsafeUtility.MemClear(s_State.Data.namePages.Ptr, (long)kNamePageCount * sizeof(IntPtr));
            Clear();
            s_State.Data.initialized = 1;
            s_State.Data.hasLoggedError = 0;
            s_State.Data.kMaxEntriesMsg = "Max unique Entity Name capacity exceeded. If you require more storage, edit EntityNameStorage.cs and change the value of kMaxEntries to pre-allocate more space.";
        }

        public static unsafe void Shutdown()
        {
            if (s_State.Data.initialized == 0)
                return;

            s_State.Data.buffer.Dispose();
            s_State.Data.entry.Dispose();
            s_State.Data.hash.Dispose();
            for (var i = 0; i < kNamePageCount; ++i)
            {
                if (s_State.Data.namePages[i] != IntPtr.Zero)
                    Memory.Unmanaged.Free((void*)s_State.Data.namePages[i], Allocator.Persistent);
            }
            s_State.Data.namePages.Dispose();
            s_State.Data.initialized = 0;
            s_State.Data.hasLoggedError = 0;
        }

        /// <summary>
        /// Clears the store.
        /// </summary>
        public static void Clear()
        {
            s_State.Data.chars = 0;
            s_State.Data.entries = 0;
            s_State.Data.hash.Clear();
            var temp = new FixedString64Bytes();
            GetOrCreateIndex(in temp); // make sure that Index=0 means empty string
        }

        /// <summary>
        /// Copies the stored FixedString64Bytes (at this index) into the ref.
        /// Asserts if out of range.
        /// </summary>
        public static unsafe void GetFixedString(int index, ref FixedString64Bytes temp)
        {
            Assert.IsTrue(index < s_State.Data.entries);
            var e = s_State.Data.entry[index];
            Assert.IsTrue(e.length <= kEntityNameMaxLengthBytes);
            temp.Length = math.min(e.length, temp.Capacity);
            UnsafeUtility.MemCpy(temp.GetUnsafePtr(), s_State.Data.buffer.Ptr + e.offset, temp.Length);
        }

        /// <remarks>
        /// You must pass in both the hash of the FixedString, and the FixedString itself.
        /// </remarks>
        /// <returns>If found, returns the index of the name. Otherwise, -1 and default(FixedString64Bytes).</returns>
        public static int GetIndexFromHashAndFixedString(int hash, in FixedString64Bytes fixedString)
        {
            Assert.IsTrue(fixedString.Length <= kEntityNameMaxLengthBytes);
            Assert.AreEqual(hash, fixedString.GetHashCode()); // The inputted hash must be the hash of the FixedString.
            int itemIndex;
            NativeParallelMultiHashMapIterator<int> iter;
            if (s_State.Data.hash.TryGetFirstValue(hash, out itemIndex, out iter))
            {
                do
                {
                    var e = s_State.Data.entry[itemIndex];
                    Assert.IsTrue(e.length <= kEntityNameMaxLengthBytes);
                    if (e.length == fixedString.Length)
                    {
                        int matches;
                        for (matches = 0; matches < e.length; ++matches)
                            if (fixedString[matches] != s_State.Data.buffer[e.offset + matches])
                                break;
                        if (matches == fixedString.Length)
                            return itemIndex;
                    }
                } while (s_State.Data.hash.TryGetNextValue(out itemIndex, ref iter));
            }
            return -1;
        }

        /// <returns>Returns true if the Store contains this FixedString.</returns>
        /// <see cref="GetIndexFromHashAndFixedString"/>
        public static bool Contains(in FixedString64Bytes value)
        {
            int h = value.GetHashCode();
            return GetIndexFromHashAndFixedString(h, in value) != -1;
        }

        /// <returns>Returns true if the Store contains this name (in FixedString form).</returns>
        /// <see cref="GetIndexFromHashAndFixedString"/>
        [ExcludeFromBurstCompatTesting("Takes managed string")]
        public static unsafe bool Contains(string value)
        {
            FixedString64Bytes temp = value;
            return Contains(in temp);
        }

        /// <summary>
        /// If the store contains this name already, returns the index of it.
        /// Otherwise, assigns the next free index, and saves the name into the store.
        /// </summary>
        /// <returns>New or existing index of value (i.e. name).</returns>
        public static int GetOrCreateIndex(in FixedString64Bytes value)
        {
            int h = value.GetHashCode();
            var itemIndex = GetIndexFromHashAndFixedString(h, in value);

            if (itemIndex != kErrorExceedMaxEntryCapacity)
                return itemIndex;

            //return an invalid value when table entry is full
            if (s_State.Data.entries >= kMaxEntries)
                return kErrorExceedMaxEntryCapacity;

            Assert.IsTrue(s_State.Data.chars + value.Length <= kMaxChars);
            var o = s_State.Data.chars;
            var l = (ushort)value.Length;
            for (var i = 0; i < l; ++i)
                s_State.Data.buffer[s_State.Data.chars++] = value[i];
            s_State.Data.entry[s_State.Data.entries] = new Entry { offset = o, length = l };
            s_State.Data.hash.Add(h, s_State.Data.entries);
            return s_State.Data.entries++;
        }

        static unsafe IntPtr* NamePageSlot(int entityIndex)
        {
            // Debugger_GetName reaches here with an entity it has not validated when it has no store
            // to validate against, so the index can be anything.
            if ((uint)entityIndex >= 1u << kEntityIndexBits)
                return null;

            return (IntPtr*)s_State.Data.namePages.Ptr + (entityIndex >> kIndicesPerPageShift);
        }

        static unsafe int* GetNamePage(int entityIndex)
        {
            var slot = NamePageSlot(entityIndex);
            if (slot == null)
                return null;

            // A page is cleared before its pointer is published, so this read has to be ordered
            // against that store or the page can be read before the clear is visible.
            return (int*)System.Threading.Volatile.Read(ref *slot);
        }

        static unsafe int* GetOrCreateNamePage(int entityIndex)
        {
            var slot = NamePageSlot(entityIndex);
            if (slot == null)
                return null;

            var page = (int*)System.Threading.Volatile.Read(ref *slot);
            if (page != null)
                return page;

            var allocated = (int*)Memory.Unmanaged.Allocate((long)kIndicesPerPage * sizeof(int), 64, Allocator.Persistent);
            UnsafeUtility.MemClear(allocated, (long)kIndicesPerPage * sizeof(int));

            // Losing the race means another thread published first.
            var won = System.Threading.Interlocked.CompareExchange(ref *slot, (IntPtr)allocated, IntPtr.Zero);
            if (won != IntPtr.Zero)
            {
                Memory.Unmanaged.Free(allocated, Allocator.Persistent);
                return (int*)won;
            }

            return allocated;
        }

        public static unsafe EntityName GetEntityName(Entity entity)
        {
            var page = GetNamePage(entity.Index);
            if (page == null)
                return default;

            return new EntityName { Index = page[entity.Index & kIndexInPageMask] };
        }

        public static unsafe void SetEntityName(Entity entity, EntityName name)
        {
            if (name.Index == 0)
                return;

            var page = GetOrCreateNamePage(entity.Index);
            if (page != null)
                page[entity.Index & kIndexInPageMask] = name.Index;
        }

        public static unsafe void RemoveEntitiesFromNameMap(Entity* entities, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                var page = GetNamePage(entities[i].Index);
                if (page != null)
                    page[entities[i].Index & kIndexInPageMask] = 0;
            }
        }
    }

    /// <remarks>
    /// An "EntityName" is an integer that refers to 4,096 or fewer chars of UTF-16 text in a global storage blob.
    /// Each should refer to *at most* about one printed page of text.
    ///
    /// If you need more text, consider using one EntityName struct for each printed page's worth.
    ///
    /// Each EntityName instance that you create is stored in a single, internally-managed EntityNameStorage object,
    /// which can hold up to 16,384 EntityName entries. Once added, the entries in EntityNameStorage cannot be modified
    /// or removed.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    internal struct EntityName
    {
        internal int Index;

        /// <summary>
        /// Writes the FixedString at this corresponding <see cref="Index"/> into value.
        /// </summary>
        public void ToFixedString(ref FixedString64Bytes value)
        {
            EntityNameStorage.GetFixedString(Index, ref value);
        }

        /// <summary>
        /// System.String equivalent of <see cref="ToFixedString"/>.
        /// </summary>
        [ExcludeFromBurstCompatTesting("Returns managed string")]
        public override string ToString()
        {
            FixedString64Bytes temp = default;
            ToFixedString(ref temp);
            return temp.ToString();
        }

        /// <summary>
        /// Writes the FixedString value into the store, cached as an <see cref="Index"/>.
        /// </summary>
        public void SetFixedString(in FixedString64Bytes value)
        {
            int tryIndex = EntityNameStorage.GetOrCreateIndex(in value);

            if(tryIndex >= 0)
                Index = tryIndex;
            else if(EntityNameStorage.s_State.Data.hasLoggedError == 0)
            {
                // The unmanaged overload logs a FixedString from Burst-compiled callers too
                var message = EntityNameStorage.s_State.Data.kMaxEntriesMsg;
                unsafe
                {
                    Unity.Scripting.LowLevel.Debug.LogError__Unmanaged(message.GetUnsafePtr(), message.Length);
                }
                EntityNameStorage.s_State.Data.hasLoggedError++;
            }
        }
    }
}
