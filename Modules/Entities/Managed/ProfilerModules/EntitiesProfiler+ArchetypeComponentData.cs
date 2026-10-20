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
        /// Struct used to store component type information for archetypes.
        /// Each record represents one component type in one archetype.
        /// Records are linked to archetypes via ArchetypeStableHash.
        /// </summary>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_PROFILER")]
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        public readonly struct ArchetypeComponentData : IEquatable<ArchetypeComponentData>
        {
            [FieldOffset(0)] // 8 bytes - Archetype this component belongs to
            public readonly ulong ArchetypeStableHash;

            [FieldOffset(8)] // 8 bytes - Component type identifier
            public readonly ulong ComponentStableTypeHash;

            [FieldOffset(16)] // 4 bytes
            public readonly ComponentTypeFlags Flags;

            [FieldOffset(20)] // 4 bytes - Order of component in archetype
            public readonly int IndexInArchetype;

            public ArchetypeComponentData(ulong archetypeStableHash, ulong componentStableTypeHash, ComponentTypeFlags flags, int indexInArchetype)
            {
                ArchetypeStableHash = archetypeStableHash;
                ComponentStableTypeHash = componentStableTypeHash;
                Flags = flags;
                IndexInArchetype = indexInArchetype;
            }

            public TypeIndex GetTypeIndex()
            {
                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(ComponentStableTypeHash);
                if (typeIndex == TypeIndex.Null)
                    return typeIndex;

                if ((Flags & ComponentTypeFlags.ChunkComponent) != 0)
                    typeIndex = TypeManager.MakeChunkComponentTypeIndex(typeIndex);

                return typeIndex;
            }

            public bool Equals(ArchetypeComponentData other)
            {
                return ArchetypeStableHash == other.ArchetypeStableHash &&
                       ComponentStableTypeHash == other.ComponentStableTypeHash &&
                       Flags == other.Flags &&
                       IndexInArchetype == other.IndexInArchetype;
            }

            [ExcludeFromBurstCompatTesting("Takes managed object")]
            public override bool Equals(object obj)
            {
                return obj is ArchetypeComponentData other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = ArchetypeStableHash.GetHashCode();
                    hashCode = (hashCode * 397) ^ IndexInArchetype;
                    return hashCode;
                }
            }

            public static bool operator ==(ArchetypeComponentData lhs, ArchetypeComponentData rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(ArchetypeComponentData lhs, ArchetypeComponentData rhs)
            {
                return !lhs.Equals(rhs);
            }
        }
    }
}
