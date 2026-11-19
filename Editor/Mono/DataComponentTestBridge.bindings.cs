// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // Test-only: gives a GameObject data components in its entity's chunk, for test assemblies without access to
    // Unity.Entities.
    [NativeHeader("Editor/Src/Utility/DataComponentTestBridge.bindings.h")]
    internal static class DataComponentTestBridge
    {
        internal static uint GetComponentTypeId(Type componentType)
        {
            if (componentType == null)
                throw new ArgumentNullException(nameof(componentType));
            return GetOrMintComponentTypeIdNative(componentType.TypeHandle.Value);
        }

        [FreeFunction("DataComponentTestBridge::GetOrMintComponentTypeId")]
        static extern uint GetOrMintComponentTypeIdNative(IntPtr backendTypePtr);

        [FreeFunction("DataComponentTestBridge::RemoveComponent")]
        internal static extern bool RemoveComponent(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::RemoveComponentFromChunk")]
        internal static extern bool RemoveComponentFromChunk(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::AddDataComponentToGameObject")]
        internal static extern EntityId AddDataComponentToGameObject(GameObject gameObject, uint componentTypeId);

        [FreeFunction("DataComponentTestBridge::WriteComponentBytes")]
        internal static extern bool WriteComponentBytes(EntityId componentEntityId, IntPtr src, uint size);

        [FreeFunction("DataComponentTestBridge::ReadComponentBytes")]
        internal static extern bool ReadComponentBytes(EntityId componentEntityId, IntPtr dst, uint size);

        [FreeFunction("DataComponentTestBridge::HasComponent")]
        internal static extern bool HasComponent(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::GameObjectListsComponent")]
        internal static extern bool GameObjectListsComponent(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::GetComponentCount")]
        internal static extern uint GetComponentCount(EntityId ownerId);

        [FreeFunction("DataComponentTestBridge::GetComponentAt")]
        internal static extern EntityId GetComponentAt(EntityId ownerId, uint index);

        [FreeFunction("DataComponentTestBridge::GetComponentListEntityIds")]
        internal static extern EntityId[] GetComponentListEntityIds(GameObject gameObject);

        [FreeFunction("DataComponentTestBridge::GetBufferLength")]
        internal static extern int GetBufferLength(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::ReadBufferElements")]
        internal static extern bool ReadBufferElements(EntityId componentEntityId, IntPtr dst, uint elementSize, uint count);

        [FreeFunction("DataComponentTestBridge::SetBuffer")]
        internal static extern bool SetBuffer(EntityId componentEntityId, IntPtr src, uint elementSize, uint count);

        [FreeFunction("DataComponentTestBridge::GetCorrespondingSourceEntityId")]
        internal static extern EntityId GetCorrespondingSourceEntityId(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::SetCorrespondingSourceEntityId")]
        internal static extern void SetCorrespondingSourceEntityId(EntityId componentEntityId, EntityId source);

        [FreeFunction("DataComponentTestBridge::GetPrefabInstanceEntityId")]
        internal static extern EntityId GetPrefabInstanceEntityId(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::GetPrefabAssetEntityId")]
        internal static extern EntityId GetPrefabAssetEntityId(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::SetPrefabInstanceEntityId")]
        internal static extern void SetPrefabInstanceEntityId(EntityId componentEntityId, EntityId prefabInstance);

        [FreeFunction("DataComponentTestBridge::SetPrefabAssetEntityId")]
        internal static extern void SetPrefabAssetEntityId(EntityId componentEntityId, EntityId prefabAsset);

        [FreeFunction("DataComponentTestBridge::GetHideFlags")]
        internal static extern uint GetHideFlags(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::SetHideFlags")]
        internal static extern void SetHideFlags(EntityId componentEntityId, uint flags);

        [FreeFunction("DataComponentTestBridge::GetFileIDHint")]
        internal static extern long GetFileIDHint(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::SetFileIDHint")]
        internal static extern void SetFileIDHint(EntityId componentEntityId, long hint);

        [FreeFunction("DataComponentTestBridge::GetLocalFileID")]
        internal static extern long GetLocalFileID(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::AddDataComponentUndoable")]
        internal static extern bool AddDataComponentUndoable(EntityId ownerId, uint componentTypeId, string actionName);

        [FreeFunction("DataComponentTestBridge::RegisterDataComponentPropertyUndo")]
        internal static extern bool RegisterDataComponentPropertyUndo(EntityId[] componentIds, string actionName);

        [FreeFunction("DataComponentTestBridge::GetDataComponentOwner")]
        internal static extern GameObject GetDataComponentOwner(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::DestroyObjectUndoable")]
        internal static extern void DestroyObjectUndoable(EntityId id, string actionName);

        [FreeFunction("DataComponentTestBridge::IsPropertyChangeRecordASceneUndo")]
        internal static extern bool IsPropertyChangeRecordASceneUndo(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::RegisterPropertyChangeRecord")]
        internal static extern bool RegisterPropertyChangeRecord(EntityId componentEntityId);

        [FreeFunction("DataComponentTestBridge::WriteComponentStream")]
        internal static extern byte[] WriteComponentStream(EntityId componentEntityId, bool throughRedirect, bool toSerializedFile, long ownerLocalIdentifier);

        [FreeFunction("DataComponentTestBridge::ReadComponentStreamThroughRedirect")]
        internal static extern void ReadComponentStreamThroughRedirect(EntityId componentEntityId, byte[] stream);
    }
}
