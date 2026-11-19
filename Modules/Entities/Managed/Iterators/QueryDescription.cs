// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Unity.Entities
{
    // World-independent description of one archetype query. One allocation: this header, then the All, Any, None,
    // Disabled, Absent and Present arrays, each sorted by TypeIndex. Access modes are not part of it.
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct QueryDescription
    {
        public uint TypesHash;
        public EntityQueryOptions Options;
        public ushort AllCount;
        public ushort AnyCount;
        public ushort NoneCount;
        public ushort DisabledCount;
        public ushort AbsentCount;
        public ushort PresentCount;

        public TypeIndex* All
        {
            get
            {
                fixed (QueryDescription* self = &this)
                {
                    return (TypeIndex*)(self + 1);
                }
            }
        }

        public TypeIndex* Any => All + AllCount;
        public TypeIndex* None => Any + AnyCount;
        public TypeIndex* Disabled => None + NoneCount;
        public TypeIndex* Absent => Disabled + DisabledCount;
        public TypeIndex* Present => Absent + AbsentCount;

        public int TypeCount => AllCount + AnyCount + NoneCount + DisabledCount + AbsentCount + PresentCount;
        public int SizeInBytes => sizeof(QueryDescription) + TypeCount * sizeof(TypeIndex);

        static int CalculateSizeInBytes(in ArchetypeQuery archetypeQuery)
        {
            var typeCount = archetypeQuery.AllCount + archetypeQuery.AnyCount + archetypeQuery.NoneCount +
                            archetypeQuery.DisabledCount + archetypeQuery.AbsentCount + archetypeQuery.PresentCount;
            return sizeof(QueryDescription) + typeCount * sizeof(TypeIndex);
        }

        // One block: the array of description pointers, then the descriptions. Free it with Free().
        internal static QueryDescription** Allocate(ArchetypeQuery* archetypeQueries, int count)
        {
            var size = sizeof(QueryDescription*) * count;
            for (int i = 0; i < count; ++i)
                size += CalculateSizeInBytes(archetypeQueries[i]);

            var memoryLabel = Memory.CreateLabel("Entities", "Query.Description", Allocator.Persistent);
            var block = (byte*)Memory.Unmanaged.Allocate(size, 8, memoryLabel);
            var descriptions = (QueryDescription**)block;
            var next = block + sizeof(QueryDescription*) * count;
            for (int i = 0; i < count; ++i)
            {
                ref var archetypeQuery = ref archetypeQueries[i];
                var description = (QueryDescription*)next;
#pragma warning disable 0618 // Write groups are obsolete. The builder has already expanded them into None.
                description->Options = archetypeQuery.Options & ~EntityQueryOptions.FilterWriteGroup;
#pragma warning restore 0618
                description->AllCount = (ushort)archetypeQuery.AllCount;
                description->AnyCount = (ushort)archetypeQuery.AnyCount;
                description->NoneCount = (ushort)archetypeQuery.NoneCount;
                description->DisabledCount = (ushort)archetypeQuery.DisabledCount;
                description->AbsentCount = (ushort)archetypeQuery.AbsentCount;
                description->PresentCount = (ushort)archetypeQuery.PresentCount;
                UnsafeUtility.MemCpy(description->All, archetypeQuery.All, archetypeQuery.AllCount * sizeof(TypeIndex));
                UnsafeUtility.MemCpy(description->Any, archetypeQuery.Any, archetypeQuery.AnyCount * sizeof(TypeIndex));
                UnsafeUtility.MemCpy(description->None, archetypeQuery.None, archetypeQuery.NoneCount * sizeof(TypeIndex));
                UnsafeUtility.MemCpy(description->Disabled, archetypeQuery.Disabled, archetypeQuery.DisabledCount * sizeof(TypeIndex));
                UnsafeUtility.MemCpy(description->Absent, archetypeQuery.Absent, archetypeQuery.AbsentCount * sizeof(TypeIndex));
                UnsafeUtility.MemCpy(description->Present, archetypeQuery.Present, archetypeQuery.PresentCount * sizeof(TypeIndex));
                description->TypesHash = math.hash((byte*)description + sizeof(uint), description->SizeInBytes - sizeof(uint));

                descriptions[i] = description;
                next += description->SizeInBytes;
            }

            return descriptions;
        }

        // Same block layout as Allocate, holding one copy of source.
        internal static QueryDescription** AllocateCopy(QueryDescription* source)
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Query.Description", Allocator.Persistent);
            var block = (byte*)Memory.Unmanaged.Allocate(sizeof(QueryDescription*) + source->SizeInBytes, 8, memoryLabel);
            var descriptions = (QueryDescription**)block;
            descriptions[0] = (QueryDescription*)(block + sizeof(QueryDescription*));
            UnsafeUtility.MemCpy(descriptions[0], source, source->SizeInBytes);
            return descriptions;
        }

        internal static void Free(QueryDescription** descriptions)
        {
            var memoryLabel = Memory.CreateLabel("Entities", "Query.Description", Allocator.Persistent);
            Memory.Unmanaged.Free(descriptions, memoryLabel);
        }
    }
}
