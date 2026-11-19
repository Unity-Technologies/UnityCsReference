// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Entities
{
    // Identifies an archetype by its slot in EntityComponentStore.m_Archetypes and its creation number. It stays
    // valid while the slot holds the same archetype.
    internal struct ArchetypeRef
    {
        public int Slot;
        public uint Version;
    }
}
