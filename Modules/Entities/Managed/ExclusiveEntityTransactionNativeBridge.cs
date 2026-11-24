// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    // The native ExclusiveEntityTransaction (Modules/Entities/Public/ExclusiveEntityTransaction.h) calls these with a
    // pointer to itself, which has the managed layout. Types are named as for GlobalEntityManagerNativeBridge.
    static unsafe class ExclusiveEntityTransactionNativeBridge
    {
        static ExclusiveEntityTransaction* ToTransaction(IntPtr transaction) => (ExclusiveEntityTransaction*)transaction;

        static Entity ToEntity(ulong entityId) => GlobalEntityManagerNativeBridge.ToEntity(entityId);

        static ComponentType ToComponentType(uint typeId) => GlobalEntityManagerNativeBridge.ToComponentType(typeId);

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static bool Exists(IntPtr transaction, ulong entity) => ToTransaction(transaction)->Exists(ToEntity(entity));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static bool AttachGameObjectEntity(IntPtr transaction, ulong entity) =>
            ToTransaction(transaction)->AttachGameObjectEntity(ToEntity(entity));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static bool HasComponent(IntPtr transaction, ulong entity, uint typeId) =>
            ToTransaction(transaction)->HasComponent(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void AddComponent(IntPtr transaction, ulong entity, uint typeId) =>
            ToTransaction(transaction)->AddComponent(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void RemoveComponent(IntPtr transaction, ulong entity, uint typeId) =>
            ToTransaction(transaction)->RemoveComponent(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static IntPtr GetComponentDataPtrRO(IntPtr transaction, ulong entity, uint typeId) =>
            (IntPtr)ToTransaction(transaction)->GetComponentDataPtrRO(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static IntPtr GetComponentDataPtrRW(IntPtr transaction, ulong entity, uint typeId) =>
            (IntPtr)ToTransaction(transaction)->GetComponentDataPtrRW(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void GetBuffer(IntPtr transaction, ulong entity, uint typeId, bool isReadOnly, IntPtr buffer) =>
            *(UnsafeUntypedDynamicBuffer*)buffer = ToTransaction(transaction)->GetBuffer(ToEntity(entity), ToComponentType(typeId), isReadOnly);

        // The state crosses as native's GlobalEntityManager::ComponentObjectState, which has the same layout
        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void GetComponentObjectState(IntPtr transaction, ulong entity, uint typeId, IntPtr state) =>
            *(ComponentObjectState*)state = ToTransaction(transaction)->GetComponentObjectState(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static void SetComponentObjectState(IntPtr transaction, ulong entity, uint typeId, IntPtr state) =>
            ToTransaction(transaction)->SetComponentObjectState(ToEntity(entity), ToComponentType(typeId), *(ComponentObjectState*)state);

        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static bool HasComponentObjectState(IntPtr transaction, ulong entity, uint typeId) =>
            ToTransaction(transaction)->HasComponentObjectState(ToEntity(entity), ToComponentType(typeId));

        // Writes up to capacity ids and returns the entity's type count, so the caller can retry with a larger buffer
        [RequiredByNativeCode(GenerateNativeLookup = false)]
        internal static int GetComponentTypes(IntPtr transaction, ulong entity, IntPtr typeIds, int capacity)
        {
            var types = ToTransaction(transaction)->GetComponentTypes(ToEntity(entity), Allocator.Temp);
            var count = Math.Min(types.Length, capacity);
            for (var i = 0; i < count; ++i)
            {
                ((uint*)typeIds)[i] = GlobalEntityManagerNativeBridge.ToTypeId(types[i]);
            }
            return types.Length;
        }
    }
}
