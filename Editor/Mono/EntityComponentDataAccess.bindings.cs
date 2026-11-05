// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Test-only bridge for the mock chunk store. Exposes just enough surface
    // for managed tests to build a hybrid setup (real GameObject + component
    // row in the mock) without a full ECS baker. Slated for deletion when the
    // real chunk-storage primitives become drivable from managed.
    [NativeHeader("Editor/Src/Utility/EntityComponentDataAccessTestBridge.bindings.h")]
    internal static class EntityComponentDataAccessTestBridge
    {
        internal static uint GetComponentTypeId(Type componentType)
        {
            if (componentType == null)
                throw new ArgumentNullException(nameof(componentType));
            return GetOrMintComponentTypeIdNative(componentType.TypeHandle.Value);
        }

        [FreeFunction("EntityComponentDataAccessTestBridge::GetOrMintComponentTypeId")]
        static extern uint GetOrMintComponentTypeIdNative(IntPtr backendTypePtr);

        [FreeFunction("EntityComponentDataAccessTestBridge::RemoveComponent")]
        internal static extern bool RemoveComponent(EntityId componentEntityId);

        [FreeFunction("EntityComponentDataAccessTestBridge::AddDataComponentToGameObject")]
        internal static extern EntityId AddDataComponentToGameObject(GameObject gameObject, uint componentTypeId);

        [FreeFunction("EntityComponentDataAccessTestBridge::WriteComponentBytes")]
        internal static extern bool WriteComponentBytes(EntityId componentEntityId, IntPtr src, uint size);

        [FreeFunction("EntityComponentDataAccessTestBridge::ReadComponentBytes")]
        internal static extern bool ReadComponentBytes(EntityId componentEntityId, IntPtr dst, uint size);

        [FreeFunction("EntityComponentDataAccessTestBridge::HasComponent")]
        internal static extern bool HasComponent(EntityId componentEntityId);

        [FreeFunction("EntityComponentDataAccessTestBridge::GetComponentCount")]
        internal static extern uint GetComponentCount(EntityId ownerId);

        [FreeFunction("EntityComponentDataAccessTestBridge::GetComponentAt")]
        internal static extern EntityId GetComponentAt(EntityId ownerId, uint index);

        [FreeFunction("EntityComponentDataAccessTestBridge::ClearAllComponents")]
        internal static extern void ClearAllComponents();

        [FreeFunction("EntityComponentDataAccessTestBridge::RemapDataComponentTypeId")]
        internal static extern bool RemapDataComponentTypeId(EntityId owner, uint oldTypeId, uint newTypeId);

        // EntityComponentDataAccess::EntityManager, for entities in a World.
        [FreeFunction("EntityComponentDataAccessTestBridge::EntityManagerAddComponent")]
        internal static extern bool EntityManagerAddComponent(EntityId entityId, uint componentTypeId);

        // 1 enabled, 0 disabled, -1 if the entity does not have the component or its type is not enableable.
        [FreeFunction("EntityComponentDataAccessTestBridge::EntityManagerTryGetComponentEnabled")]
        internal static extern int EntityManagerTryGetComponentEnabled(EntityId entityId, uint componentTypeId);

        [FreeFunction("EntityComponentDataAccessTestBridge::EntityManagerSetComponentEnabled")]
        internal static extern bool EntityManagerSetComponentEnabled(EntityId entityId, uint componentTypeId, bool enabled);
    }
}
