// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    // Bit layout of a data-component EntityId, mirroring EntityId.h: the 12-bit TypeId field
    // at bit 28 is non-zero for a data component, and clearing it gives the owning GameObject's EntityId.
    internal static class DataComponentEntityId
    {
        const int kTypeIdShift = 28;
        const ulong kTypeIdMask = 0xFFFUL << kTypeIdShift;

        internal static bool IsDataComponent(EntityId id) => (EntityId.ToULong(id) & kTypeIdMask) != 0;

        internal static EntityId GetOwner(EntityId componentId) => EntityId.FromULong(EntityId.ToULong(componentId) & ~kTypeIdMask);

        internal static uint GetTypeId(EntityId componentId) => (uint)((EntityId.ToULong(componentId) & kTypeIdMask) >> kTypeIdShift);

        internal static EntityId Make(EntityId ownerId, uint typeId) => EntityId.FromULong(EntityId.ToULong(GetOwner(ownerId)) | ((ulong)typeId << kTypeIdShift));
    }
}
