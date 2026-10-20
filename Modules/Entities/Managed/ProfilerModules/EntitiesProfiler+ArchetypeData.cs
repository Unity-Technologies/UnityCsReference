// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Unity.Entities
{
    partial class EntitiesProfiler
    {
        /// <summary>
        /// Struct used to store per archetype metadata.
        /// Component types are stored separately in ArchetypeComponentData records.
        /// </summary>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        [StructLayout(LayoutKind.Explicit, Size = 32)]
        public unsafe struct ArchetypeData : IEquatable<ArchetypeData>
        {
            [FieldOffset(0)] // 8 bytes - The archetypes hash
            public readonly ulong StableHash;

            [FieldOffset(8)] // 4 bytes - Maximum number of Entities which can fit within a chunk of this Archetype
            public readonly int ChunkCapacity;

            [FieldOffset(12)] // 4 bytes - Size (in bytes) for one instance of the Archetype
            public readonly int InstanceSize;

            [FieldOffset(16)] // 4 bytes - Number of components the Archetype contains
            public readonly int ComponentTypeCount;

            public ArchetypeData(Archetype* archetype)
            {
                StableHash = archetype->StableHash;
                ChunkCapacity = archetype->ChunkCapacity;
                InstanceSize = archetype->InstanceSize;
                ComponentTypeCount = archetype->TypesCount;
            }

            public bool Equals(ArchetypeData other)
            {
                return StableHash == other.StableHash;
            }

            [ExcludeFromBurstCompatTesting("Takes managed object")]
            public override bool Equals(object obj)
            {
                return obj is ArchetypeData archetypeData && Equals(archetypeData);
            }

            public override int GetHashCode()
            {
                return StableHash.GetHashCode();
            }

            public static bool operator ==(ArchetypeData lhs, ArchetypeData rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(ArchetypeData lhs, ArchetypeData rhs)
            {
                return !lhs.Equals(rhs);
            }
        }
    }
}
