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

        [FreeFunction("EntityComponentDataAccessTestBridge::AddComponent")]
        internal static extern bool AddComponent(EntityId parentId, uint componentTypeId);

        [FreeFunction("EntityComponentDataAccessTestBridge::HasComponent")]
        internal static extern bool HasComponent(EntityId componentEntityId);

        [FreeFunction("EntityComponentDataAccessTestBridge::ClearAllComponents")]
        internal static extern void ClearAllComponents();

        [FreeFunction("EntityComponentDataAccessTestBridge::RemapDataComponentTypeId")]
        internal static extern bool RemapDataComponentTypeId(EntityId parent, uint oldTypeId, uint newTypeId);
    }
}
