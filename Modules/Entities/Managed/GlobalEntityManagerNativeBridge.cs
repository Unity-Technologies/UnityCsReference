// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    // The native GlobalEntityManager (Modules/Entities/Public/GlobalEntityManager.h) calls these. Native code names a
    // component type by its EntityId type id, which is the TypeIndex without its flag bits. A chunk component keeps
    // TypeManager.ChunkComponentTypeFlag, because nothing else tells it apart from the regular component of its type.
    static unsafe class GlobalEntityManagerNativeBridge
    {
        static Entity ToEntity(ulong entityId) => UnsafeUtility.As<ulong, Entity>(ref entityId);

        // Type ids are sparse, and an id without a registered type finds the null type's TypeInfo
        static ComponentType ToComponentType(uint typeId)
        {
            var isChunkComponent = (typeId & TypeManager.ChunkComponentTypeFlag) != 0;
            var index = (int)(typeId & ~(uint)TypeManager.ChunkComponentTypeFlag);
            var typeIndex = TypeManager.GetTypeInfo(new TypeIndex { Value = index }).TypeIndex;
            if (index == 0 || typeIndex.Index != index)
                throw new ArgumentException($"{typeId} isn't a registered component type id.");
            return ComponentType.FromTypeIndex(isChunkComponent ? TypeManager.MakeChunkComponentTypeIndex(typeIndex) : typeIndex);
        }

        static uint ToTypeId(ComponentType type) => (uint)(type.TypeIndex.Index | (type.IsChunkComponent ? TypeManager.ChunkComponentTypeFlag : 0));

        static ComponentTypeSet ToComponentTypeSet(IntPtr typeIds, int count)
        {
            var types = new FixedList128Bytes<ComponentType>();
            for (var i = 0; i < count; ++i)
            {
                types.Add(ToComponentType(((uint*)typeIds)[i]));
            }
            return new ComponentTypeSet(types);
        }

        [RequiredByNativeCode]
        internal static void GetWorldUnmanaged(ulong entity, IntPtr world) =>
            *(WorldUnmanaged*)world = GlobalEntityManager.GetWorldUnmanaged(ToEntity(entity));

        [RequiredByNativeCode]
        internal static bool Exists(ulong entity) => GlobalEntityManager.Exists(ToEntity(entity));

        [RequiredByNativeCode]
        internal static bool HasComponent(ulong entity, uint typeId) => GlobalEntityManager.HasComponent(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static bool AddComponent(ulong entity, uint typeId) => GlobalEntityManager.AddComponent(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static bool RemoveComponent(ulong entity, uint typeId) => GlobalEntityManager.RemoveComponent(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static void AddComponents(ulong entity, IntPtr typeIds, int count) => GlobalEntityManager.AddComponent(ToEntity(entity), ToComponentTypeSet(typeIds, count));

        [RequiredByNativeCode]
        internal static void RemoveComponents(ulong entity, IntPtr typeIds, int count) => GlobalEntityManager.RemoveComponent(ToEntity(entity), ToComponentTypeSet(typeIds, count));

        [RequiredByNativeCode]
        internal static IntPtr GetComponentDataPtrRO(ulong entity, uint typeId) => (IntPtr)GlobalEntityManager.GetComponentDataPtrRO(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static IntPtr GetComponentDataPtrRW(ulong entity, uint typeId) => (IntPtr)GlobalEntityManager.GetComponentDataPtrRW(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static IntPtr GetChunkComponentDataPtrRO(ulong entity, uint typeId) => (IntPtr)GlobalEntityManager.GetChunkComponentDataPtrRO(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static IntPtr GetChunkComponentDataPtrRW(ulong entity, uint typeId) => (IntPtr)GlobalEntityManager.GetChunkComponentDataPtrRW(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static void GetBuffer(ulong entity, uint typeId, bool isReadOnly, IntPtr buffer) =>
            *(UnsafeUntypedDynamicBuffer*)buffer = GlobalEntityManager.GetBuffer(ToEntity(entity), ToComponentType(typeId), isReadOnly);

        [RequiredByNativeCode]
        internal static IntPtr GetSharedComponentPtrRO(ulong entity, uint typeId) => (IntPtr)GlobalEntityManager.GetSharedComponentPtrRO(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static void SetSharedComponent(ulong entity, uint typeId, IntPtr componentData) => GlobalEntityManager.SetSharedComponent(ToEntity(entity), ToComponentType(typeId), (void*)componentData);

        [RequiredByNativeCode]
        internal static bool IsComponentEnabled(ulong entity, uint typeId) => GlobalEntityManager.IsComponentEnabled(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static void SetComponentEnabled(ulong entity, uint typeId, bool value) => GlobalEntityManager.SetComponentEnabled(ToEntity(entity), ToComponentType(typeId), value);

        [RequiredByNativeCode]
        internal static uint GetChangeVersion(ulong entity, uint typeId) => GlobalEntityManager.GetChangeVersion(ToEntity(entity), ToComponentType(typeId));

        // The state crosses as native's GlobalEntityManager::ComponentObjectState, which has the same layout
        [RequiredByNativeCode]
        internal static void GetComponentObjectState(ulong entity, uint typeId, IntPtr state) =>
            *(ComponentObjectState*)state = GlobalEntityManager.GetComponentObjectState(ToEntity(entity), ToComponentType(typeId));

        [RequiredByNativeCode]
        internal static void SetComponentObjectState(ulong entity, uint typeId, IntPtr state) =>
            GlobalEntityManager.SetComponentObjectState(ToEntity(entity), ToComponentType(typeId), *(ComponentObjectState*)state);

        [RequiredByNativeCode]
        internal static bool HasComponentObjectState(ulong entity, uint typeId) => GlobalEntityManager.HasComponentObjectState(ToEntity(entity), ToComponentType(typeId));

        // Writes up to capacity ids and returns the entity's type count, so the caller can retry with a larger buffer
        [RequiredByNativeCode]
        internal static int GetComponentTypes(ulong entity, IntPtr typeIds, int capacity)
        {
            var types = GlobalEntityManager.GetComponentTypes(ToEntity(entity), Allocator.Temp);
            var count = Math.Min(types.Length, capacity);
            for (var i = 0; i < count; ++i)
            {
                ((uint*)typeIds)[i] = ToTypeId(types[i]);
            }
            return types.Length;
        }
    }
}
